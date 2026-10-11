using FieldOps.Core.Models;

namespace FieldOps.Api;

public sealed class WorkOrderStore
{
    private readonly Dictionary<Guid, WorkOrder> data = new()
    {
        [Guid.Parse("11111111-1111-1111-1111-111111111111")] = new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "WO-1001", "Northside Clinic", "Replace network switch", "100 Main St", new DateTimeOffset(2026, 10, 12, 9, 0, 0, TimeSpan.Zero), WorkOrderStatus.Scheduled),
        [Guid.Parse("22222222-2222-2222-2222-222222222222")] = new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "WO-1002", "West Ridge School", "Inspect wireless coverage", "220 Oak Ave", new DateTimeOffset(2026, 10, 13, 13, 30, 0, TimeSpan.Zero), WorkOrderStatus.InProgress),
        [Guid.Parse("33333333-3333-3333-3333-333333333333")] = new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "WO-1003", "Central Warehouse", "Configure barcode scanners", "35 Industrial Dr", new DateTimeOffset(2026, 10, 14, 8, 30, 0, TimeSpan.Zero), WorkOrderStatus.New)
    };
    public IReadOnlyList<WorkOrder> All() { lock (data) return data.Values.OrderBy(x => x.Number).ToArray(); }
    public WorkOrder? Get(Guid id) { lock (data) return data.GetValueOrDefault(id); }
    public WorkOrder? Update(Guid id, UpdateWorkOrderRequest request)
    {
        lock (data)
        {
            if (!data.TryGetValue(id, out var existing)) return null;
            var updated = existing with { Summary = request.Summary.Trim(), Status = request.Status };
            data[id] = updated; return updated;
        }
    }
}