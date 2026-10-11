# 004 - Properly handling Async & Network Errors

The goal of this step is to understand how to better manage async operations, possible errors, and related as we work towards real implementations.

## Enhance Home.Razor

Replace the existing contents of `Home.razor` with the below code thta introduces additional states.

```` csharp
@page "/"

@implements IDisposable

@inject IWorkOrderService WorkOrders
@using Microsoft.Extensions.Logging
@inject ILogger<Home> Logger

<h1>Assigned work orders</h1>

<p class="muted">Your upcoming field visits</p>

<div class="toolbar">
    <input aria-label="Search work orders" placeholder="Search customer or job" @bind="search" @bind:event="oninput" />
    <button class="btn" @onclick="LoadAsync" disabled="@loading">Refresh</button>
</div>

@if (loading)
{
    <p role="status">Loading work orders…</p>
}
else if (error is not null)
{
    <div class="error" role="alert">Unable to load work orders. Please try again.</div>
}
else if (filtered.Count == 0)
{
    <p>No matching work orders found.</p>
}
else
{
    <p class="muted">@filtered.Count work order(s)</p>
    @foreach (var order in filtered)
    {
        <WorkOrderCard @key="order.Id" Order="order" />
    }
}

@code {
    private bool loading = true;
    private string search = string.Empty;
    private string? error; 

    private IReadOnlyList<WorkOrder> orders = [];
    private List<WorkOrder> filtered => orders.Where(x => x.CustomerName.Contains(search, StringComparison.OrdinalIgnoreCase) 
                    || x.Number.Contains(search, StringComparison.OrdinalIgnoreCase) 
                    || x.Summary.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();


    private readonly CancellationTokenSource lifetime = new();

    private bool disposed;
    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        if (disposed) return;
        
        loading = true; 
        error = null;
        try 
        { 
            orders = await WorkOrders.GetAllAsync(lifetime.Token); 
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) 
        { 
			return; // Cacellation requested, ignore
        }
        catch (Exception ex) 
        { 
            Logger.LogError(ex, "Failed to load work orders"); // Where does this go?
            error = "load-failed"; 
        }
        finally 
        { 
            loading = false; 
        }
    }

    public void Dispose() 
    { 
        disposed = true; 
        lifetime.Cancel(); 
        lifetime.Dispose(); 
    }
}
````

This allows us to search/filter, but is there an unusual behavior here?

## Update WorkOrderDetails.razor

Implement similar processes on the work order details

```` csharp
@page "/orders/{Id:guid}"

@implements IDisposable

@inject IWorkOrderService WorkOrders
@using Microsoft.Extensions.Logging
@inject ILogger<WorkOrderDetails> Logger

<h1>Work order details</h1>

@if (loading)
{
	<p role="status">Loading details…</p>
}
else if (error)
{

	<div class="error" role="alert">Unable to load this work order.</div>
}
else if (order is null)
{

	<p>Work order not found.</p>
}
else
{
	<div class="card">
		<div class="row"><strong>@order.Number</strong><span class="pill">@order.Status</span></div>
		<h2>@order.CustomerName</h2><p>@order.Summary</p><p><strong>Location:</strong> @order.Address</p>
		<p><strong>Scheduled:</strong> @order.ScheduledFor.ToLocalTime().ToString("f")</p>
	</div>
}

<p><a href="/">← Back to work orders</a></p>

@code {
	[Parameter]
	public Guid Id { get; set; }
	private WorkOrder? order;
	private bool loading = true;
	private bool error;
	private CancellationTokenSource? request;
	private bool disposed;

	protected override async Task OnParametersSetAsync()
	{
		// Cancel any previous request and dispose of the token source, why?
		request?.Cancel();
		request?.Dispose();
		request = new CancellationTokenSource();

		var token = request.Token;
		loading = true; error = false; order = null;
		try
		{
			order = await WorkOrders.GetByIdAsync(Id, token);
		}
		catch (OperationCanceledException) when (token.IsCancellationRequested)
		{
			return;
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Failed to load work order {WorkOrderId}", Id);
			error = true;
		}
		finally
		{
			if (!disposed && !token.IsCancellationRequested)
				loading = false;
		}
	}
	public void Dispose()
	{
		disposed = true;
		request?.Cancel();
		request?.Dispose();
	}
}
````

## Testing The Failure Modes

If you would like to test out the failures, when the application is running you can use Hot Reload to update the `SimulateFailue` and `SimulateEmpty` settings within the `MockWorkOrderService`

## Validation & What's Next

- Application should run and new filtering should work well
- Do we need all of this overhead?
- Why cancel tokens?
- How do you organize?  What do you do for Real-Code?
- What are we missing?