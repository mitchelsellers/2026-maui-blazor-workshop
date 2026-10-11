# 010 - Diagnostics and Prodcution

## Workshop Diagnostics

Simple way to track/wrap/report on items

```` csharp
namespace FieldOps.Mobile.Services;
public sealed class WorkshopDiagnostics(ILogger<WorkshopDiagnostics> logger)
{
    public async Task<T> MeasureAsync<T>(string operation, Func<Task<T>> action)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var result = await action();
            logger.LogInformation("Operation {Operation} completed in {ElapsedMs} ms", operation, timer.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Operation {Operation} failed after {ElapsedMs} ms", operation, timer.ElapsedMilliseconds);
            throw;
        }
    }
}
````

Register for DI

```` csharp
builder.Services.AddSingleton<WorkshopDiagnostics>();
````

## Diagnostics Sample Page

```` csharp
@page "/diagnostics"

@inject FieldOps.Mobile.Services.WorkshopDiagnostics Diagnostics
@inject FieldOps.Core.Services.IWorkOrderService WorkOrders

<h1>Diagnostics</h1>

<p>Measure a repository operation and inspect the debug output for structured logging.</p>

<button class="btn btn-primary" disabled="@busy" @onclick="Run">Run diagnostic</button>

@if (result is not null)
{
    <p role="status">@result</p>
}

@code {
    private bool busy;
    private string? result;
    private async Task Run()
    {
        busy = true;
        try
        {
            var orders = await Diagnostics.MeasureAsync("workorders.load", () => WorkOrders.GetAllAsync());
            result = $"Loaded {orders.Count} work orders. See debug logs for elapsed time.";
        }
        catch (Exception) { result = "Diagnostic failed. See debug logs for details."; }
        finally { busy = false; }
    }
}
````

and menu item

```` csharp
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="diagnostics">
                <span class="bi bi-plus-square-fill-nav-menu" aria-hidden="true"></span> Diagnostics
            </NavLink>
        </div>
````

# Continued Learning

- Build Release configuration on CI for each supported target; configure signing identities and store provisioning.
- Replace development HTTP endpoints with TLS-only production URLs; remove cleartext exceptions.
- Integrate actual OIDC/PKCE sign-in and server-side JWT validation/authorization.
- Partition or clear offline data by authenticated user; encrypt sensitive local data as required.
- Add schema migrations, transactional offline queue writes, conflict resolution, backoff, and telemetry.
- Avoid logging tokens, PII, and work-order notes; configure retention and consent.
- Test cold start, resume, offline, poor network, permission denial, rotation, and upgrades.
- Validate Android and iOS packaging, icons, privacy disclosures, permissions and accessibility.
- Run unit/integration/device tests; verify crash reports and alerting.
