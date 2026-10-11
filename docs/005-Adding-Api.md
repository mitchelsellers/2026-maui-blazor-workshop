# 005 Adding An API to the Mix

The goal is to add a new API and allow users to see the overall proces

## Setup the API Project

- Right click on the solution and select `Add new Project`
- Select `ASP.NET Core Web API` as the type
- Give it a name of `FieldOps.Api` and select "Next"
- Leave all other settings as their defaults
- Add a project reference to `FieldOps.Core`

### Add WorkOrder Store

To simulate storage we will create a work order store to be used by the project, add a new class `WorkorderStore` with the following content.

```` csharp
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
````

### Update Program.cs (API Project)

Setup DI & routes we need, replace the existing code with our below code

```` csharp
using FieldOps.Api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<WorkOrderStore>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

var api = app.MapGroup("/api/workorders");

api.MapGet("/", (WorkOrderStore store) => Results.Ok(store.All()));
api.MapGet("/{id:guid}", (Guid id, WorkOrderStore store) => store.Get(id) is { } order ? Results.Ok(order) : Results.NotFound());

app.Run();
````

### Validate API Project

Right click on the project and start debugging, ensure that it runs by validating the following two endpoints

- `/health`
- `/api/workorders`

### Manually starting the API project

Before we talk about Aspire or otherwise, you can run the following command from the root to start this API's

`dotnet run --project src/FieldOps.Api --urls http://0.0.0.0:5088`

## Add an API Based Service to Mobile

Create a new class under services `ApiWorkOrderService.cs` with the following content

```` csharp
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
````

## Add an API Settings Object

Within the `Services` folder add a class called `ApiSettings` with the following.

```` csharp
namespace FieldOps.Mobile.Services;

public static class ApiSettings
{
    // Android emulator routes host machine traffic via 10.0.2.2. iOS simulator uses localhost.
    // Real devices require a reachable HTTPS endpoint and trusted certificate.
    public static string BaseUrl => DeviceInfo.Platform == DevicePlatform.Android
        ? "http://10.0.2.2:5088/" : "http://localhost:5088/";
}
````

## Update MauiProgram.cs

Update the MauiProgram.cs to conditionally register the API solution, by replacing the existing Singleton registration with the following

```` csharp
//Make API useage needed
builder.Services.AddHttpClient<IWorkOrderService, ApiWorkOrderService>(client =>
{
    client.BaseAddress = new Uri(ApiSettings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(15);
});
````

You will also need to install the NuGet Package `Microsoft.Extensions.Http` for this to work using the below command.

```` powershell
Install-Package Microsoft.Extensions.Http
````

### Testing this out

- Ensure the API is running
- You should be able to run the appliation now wihtout any issue

## What's Next & Discussion

- Aspire?
- Changing URl's for production