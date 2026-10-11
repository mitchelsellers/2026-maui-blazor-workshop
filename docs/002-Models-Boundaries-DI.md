# 002 - Models, Boundaries and DI (35-45 minutes)

The goal of this step is to start with models, structures, and DI, no visiual project changes are completed at this time

## Add New Class Library

Add a new `net10` class library to your project named `FieldOps.Core` and add a reference to the mobile project.

### Add Model Classes

Create a folder within the `FieldOps.Core` project named `Models` and add the following to a file `WorkOrder.cs`

```` csharp
namespace FieldOps.Core.Models;
public enum WorkOrderStatus { New, Scheduled, InProgress, Completed }
public sealed record WorkOrder(Guid Id, string Number, string CustomerName, string Summary, string Address, DateTimeOffset ScheduledFor, WorkOrderStatus Status);
````

Create another file `UpdateWorkOrderRequest`

```` csharp
using System.ComponentModel.DataAnnotations;

namespace FieldOps.Core.Models;

public sealed class UpdateWorkOrderRequest
{
    [Required]
    [StringLength(200, MinimumLength=5)] 
    public string Summary { get; set; } = "";

    [EnumDataType(typeof(WorkOrderStatus))] 
    public WorkOrderStatus Status { get; set; }
}
````

>[!Note]
>We are using records here for brevity, we will discuss some tradeoffs in this bheavior when we discuss in class

### Add Sample Service Interface

Create another folder `services` and add the following interface

```` csharp
using FieldOps.Core.Models;

namespace FieldOps.Core.Services;

public interface IWorkOrderService
{
    Task<IReadOnlyList<WorkOrder>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<WorkOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkOrder?> UpdateAsync(Guid id, UpdateWorkOrderRequest request, CancellationToken cancellationToken = default);
}
````

## Add Mock Service Implementation

Within our Mobile application create a mock implementation of the work order service within a new folder `Services`.

```` csharp
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
````

## Wire Up DI & Prepare for Future Usage

Now we want to wire up the DI, for the sake of a mock, we can register as a Singleton, we will talk about others.

Within `MauiProgram.cs` after the `AddMauiBlazorWebView();` line add the following

```` csharp
builder.Services.AddSingleton<IWorkOrderService, MockWorkOrderService>();
````

Lastly, add the following line to the `/Components/_Imports.razor` file

```` csharp
@using FieldOps.Mobile.Services
````

Sthis will set us up for future work

## Validation & What's Next

Run the project you should see no errors, but no real change yet.

Discussion Points from this lesson

- Structure & Sharing - Why a separate assembly
- DI Registration - Manual vs. Injectio | Scope?