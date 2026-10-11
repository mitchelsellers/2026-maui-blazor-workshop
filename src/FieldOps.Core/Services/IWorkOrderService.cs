using FieldOps.Core.Models;

namespace FieldOps.Core.Services;

public interface IWorkOrderService
{
    Task<IReadOnlyList<WorkOrder>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<WorkOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkOrder?> UpdateAsync(Guid id, UpdateWorkOrderRequest request, CancellationToken cancellationToken = default);
}