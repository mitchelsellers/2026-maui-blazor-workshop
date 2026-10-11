using System.Net;
using System.Net.Http.Json;
using FieldOps.Core.Models;
using FieldOps.Core.Services;

namespace FieldOps.Mobile.Services;

public sealed class ApiWorkOrderService(HttpClient http) : IWorkOrderService
{
    public async Task<IReadOnlyList<WorkOrder>> GetAllAsync(CancellationToken cancellationToken = default)
        => await http.GetFromJsonAsync<List<WorkOrder>>("api/workorders", cancellationToken) ?? [];

    public async Task<WorkOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync($"api/workorders/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WorkOrder>(cancellationToken: cancellationToken);
    }

    public Task<WorkOrder?> UpdateAsync(Guid id, UpdateWorkOrderRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
