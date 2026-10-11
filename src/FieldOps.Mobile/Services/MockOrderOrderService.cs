using FieldOps.Core.Models;
using FieldOps.Core.Services;
namespace FieldOps.Mobile.Services;

public sealed class MockWorkOrderService : IWorkOrderService
{
    private static readonly List<WorkOrder> Orders =
    [
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "WO-1001", "Northside Clinic", "Replace network switch", "100 Main St", new DateTimeOffset(2026, 10, 12, 9, 0, 0, TimeSpan.Zero), WorkOrderStatus.Scheduled),
        new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "WO-1002", "West Ridge School", "Inspect wireless coverage", "220 Oak Ave", new DateTimeOffset(2026, 10, 13, 13, 30, 0, TimeSpan.Zero), WorkOrderStatus.InProgress),
        new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "WO-1003", "Central Warehouse", "Configure barcode scanners", "35 Industrial Dr", new DateTimeOffset(2026, 10, 14, 8, 30, 0, TimeSpan.Zero), WorkOrderStatus.New)
    ];
    public async Task<WorkOrder?> UpdateAsync(Guid id, UpdateWorkOrderRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(SimulatedLatency, cancellationToken);
        if (SimulateFailure) throw new InvalidOperationException("Simulated failure");
        lock (Orders)
        {
            var index = Orders.FindIndex(x => x.Id == id);
            if (index < 0) return null;
            Orders[index] = Orders[index] with { Summary = request.Summary.Trim(), Status = request.Status };
            return Orders[index];
        }
    }
    public bool SimulateFailure { get; set; }
    public bool SimulateEmpty { get; set; }
    public TimeSpan SimulatedLatency { get; set; } = TimeSpan.FromMilliseconds(650);
    public async Task<IReadOnlyList<WorkOrder>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(SimulatedLatency, cancellationToken);
        if (SimulateFailure) throw new InvalidOperationException("Simulated data source failure.");
        lock (Orders) return SimulateEmpty ? [] : Orders.ToArray();
    }
    public async Task<WorkOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await Task.Delay(SimulatedLatency, cancellationToken);
        if (SimulateFailure) throw new InvalidOperationException("Simulated data source failure.");
        lock (Orders) return SimulateEmpty ? null : Orders.FirstOrDefault(x => x.Id == id);
    }
}