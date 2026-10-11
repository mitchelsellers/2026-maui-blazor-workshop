# 006 - Forms & Related Functionality

The goal of this section is to get familar with EditForm, InputText, and other elements.

## Create EditWorkOrder.razor

Create a new Razor Component `EditWorkOrder.razor`

```` csharp
@page "/orders/{Id:guid}/edit"

@implements IDisposable

@inject IWorkOrderService WorkOrders
@inject NavigationManager Navigation
@using Microsoft.Extensions.Logging
@inject ILogger<EditWorkOrder> Logger

<h1>Edit work order</h1>

@if (loading)
{
	<p role="status">Loading…</p>
}
else if (notFound)
{

	<p>Work order not found.</p>
}
else
{
	<EditForm Model="model" OnValidSubmit="SaveAsync" FormName="edit-work-order">
		<DataAnnotationsValidator />
		<ValidationSummary />
		<div class="form-field">
			<label for="summary">Summary</label>
			<InputText id="summary" class="form-control" @bind-Value="model.Summary" />
			<ValidationMessage For="@(() => model.Summary)" />
		</div>
		<div class="form-field">
			<label for="status">Status</label>
			<InputSelect id="status" class="form-control" @bind-Value="model.Status">
				@foreach (var status in Enum.GetValues<WorkOrderStatus>())
				{
					<option value="@status">@status</option>
				}
			</InputSelect>
		</div>
		<button class="btn" type="submit" disabled="@saving">@(saving ? "Saving…" : "Save changes")</button>
	</EditForm>
	@if (error is not null)
	{
		<p role="alert" class="error">@error</p>
	}
}

<p><a href="/orders/@Id">← Back to details</a></p>

@code {
	[Parameter]
	public Guid Id { get; set; }
	private UpdateWorkOrderRequest model = new();
	private readonly CancellationTokenSource lifetime = new();
	private bool loading = true, saving, notFound;
	private string? error;
	protected override async Task OnParametersSetAsync()
	{
		loading = true;
		error = null;
		try
		{
			var order = await WorkOrders.GetByIdAsync(Id, lifetime.Token);
			notFound = order is null;
			if (order is not null) model = new() { Summary = order.Summary, Status = order.Status };
		}
		catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
		catch (Exception ex)
		{
			Logger.LogError(ex, "Loading edit form failed");
			error = "Unable to load order.";
		}
		finally { loading = false; }
	}
	private async Task SaveAsync()
	{
		if (saving) return;
		saving = true; error = null;
		try
		{
			var updated = await WorkOrders.UpdateAsync(Id, model, lifetime.Token);
			if (updated is null) { notFound = true; return; }
			Navigation.NavigateTo($"/orders/{Id}");
		}
		catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
		catch (Exception ex)
		{
			Logger.LogError(ex, "Saving order failed");
			error = "Unable to save. Please try again.";
		}
		finally { saving = false; }
	}
	public void Dispose() { lifetime.Cancel(); lifetime.Dispose(); }
}

````

## Add Edit Link to WorkOrderDetails

Add below the scheduled text

```` html
<p><a href="/orders/@Id/edit">Edit work order</a></p>
````

## Add the Update Implementation to the API Project

### Add the API Call to Program.cs within the API project

```` csharp
api.MapPut("/{id:guid}", (Guid id, UpdateWorkOrderRequest request, WorkOrderStore store) =>
{
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(request.Summary) || request.Summary.Trim().Length is < 5 or > 200)
        errors[nameof(request.Summary)] = ["Summary must contain between 5 and 200 characters."];
    if (!Enum.IsDefined(request.Status)) errors[nameof(request.Status)] = ["Invalid status."];
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    return store.Update(id, request) is { } order ? Results.Ok(order) : Results.NotFound();
});
````

### Update API Method for ApiWorkOrderStore

Replace the existing `NotImplemented` code with the following

```` csharp
    public async Task<WorkOrder?> UpdateAsync(Guid id, UpdateWorkOrderRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await http.PutAsJsonAsync($"api/workorders/{id}", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WorkOrder>(cancellationToken: cancellationToken);
    }
````

## Review / Test

- Start the API's
- Start the mobile application
- You should see add/update behaviors across the board
