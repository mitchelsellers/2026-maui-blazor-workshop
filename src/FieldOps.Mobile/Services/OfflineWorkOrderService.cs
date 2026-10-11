using FieldOps.Core.Models;
using FieldOps.Core.Services;
using Microsoft.Extensions.Logging;
using SQLite;
using System.Text.Json;

namespace FieldOps.Mobile.Services;

// Local-first reads and a durable pending-change queue. This is a workshop implementation,
// not a complete conflict-resolution or multi-user synchronization engine.
public sealed class OfflineWorkOrderService(IWorkOrderService remote, ILogger<OfflineWorkOrderService> logger) : IWorkOrderService
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private SQLiteAsyncConnection? db;
    private async Task<SQLiteAsyncConnection> DatabaseAsync()
    {
        if (db is not null) return db;
        await gate.WaitAsync();
        try
        {
            if (db is null)
            {
                var path = Path.Combine(FileSystem.AppDataDirectory, "fieldops-cache.db3");
                var connection = new SQLiteAsyncConnection(path);
                await connection.CreateTableAsync<CachedOrder>();
                await connection.CreateTableAsync<PendingEdit>();
                db = connection;
            }
            return db;
        }
        finally { gate.Release(); }
    }
    public async Task<IReadOnlyList<WorkOrder>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var database = await DatabaseAsync();
        var local = await database.Table<CachedOrder>().ToListAsync();
        try
        {
            // Avoid overwriting unsynchronized local edits with older server values.
            var pendingIds = (await database.Table<PendingEdit>().ToListAsync()).Select(x => x.Id).ToHashSet();
            var fresh = await remote.GetAllAsync(cancellationToken);
            foreach (var item in fresh.Where(x => !pendingIds.Contains(x.Id.ToString())))
                await database.InsertOrReplaceAsync(CachedOrder.From(item));
            local = await database.Table<CachedOrder>().ToListAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Remote refresh unavailable; serving cached orders");
            if (local.Count == 0) throw; // First run must make the lack of data explicit.
        }
        return local.Select(x => x.ToModel()).OrderBy(x => x.Number).ToArray();
    }
    public async Task<WorkOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var database = await DatabaseAsync();
        var cached = await database.FindAsync<CachedOrder>(id.ToString());
        if (cached is not null) return cached.ToModel();
        var item = await remote.GetByIdAsync(id, cancellationToken);
        if (item is not null) await database.InsertOrReplaceAsync(CachedOrder.From(item));
        return item;
    }
    public async Task<WorkOrder?> UpdateAsync(Guid id, UpdateWorkOrderRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var database = await DatabaseAsync();
        var cached = await database.FindAsync<CachedOrder>(id.ToString());
        if (cached is null) return null;
        var updated = cached.ToModel() with { Summary = request.Summary.Trim(), Status = request.Status };
        // Queue before replacing the cached record. In production, make both writes transactional.
        await database.InsertOrReplaceAsync(new PendingEdit { Id = id.ToString(), Payload = JsonSerializer.Serialize(request) });
        await database.InsertOrReplaceAsync(CachedOrder.From(updated));
        return updated;
    }
    public async Task<int> PendingCountAsync() => await (await DatabaseAsync()).Table<PendingEdit>().CountAsync();
    public async Task<int> SyncAsync(CancellationToken cancellationToken = default)
    {
        var database = await DatabaseAsync();
        var edits = await database.Table<PendingEdit>().ToListAsync();
        int synced = 0;
        foreach (var edit in edits)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var request = JsonSerializer.Deserialize<UpdateWorkOrderRequest>(edit.Payload)!;
            var updated = await remote.UpdateAsync(Guid.Parse(edit.Id), request, cancellationToken);
            if (updated is null) throw new InvalidOperationException($"Server no longer contains {edit.Id}");
            await database.InsertOrReplaceAsync(CachedOrder.From(updated));
            await database.DeleteAsync(edit);
            synced++;
        }
        return synced;
    }
    public sealed class CachedOrder
    {
        [PrimaryKey] public string Id { get; set; } = "";
        public string Json { get; set; } = "";
        public static CachedOrder From(WorkOrder order) => new() { Id = order.Id.ToString(), Json = JsonSerializer.Serialize(order) };
        public WorkOrder ToModel() => JsonSerializer.Deserialize<WorkOrder>(Json)!;
    }
    public sealed class PendingEdit
    {
        [PrimaryKey] public string Id { get; set; } = "";
        public string Payload { get; set; } = "";
    }
}