# 003 - Components and Navigation

Discovering how things work to get data into our project and how we can interact with them

## Update _Imports.razor

Correct a typo from the prior changes for the Imports to import `FieldOps.Core.Services`, as well as allow models to be used, the final two lines should now read.

```` csharp
@using FieldOps.Core.Models
@using FieldOps.Core.Services

````

## Initial Display of Information

We are going to start out by using our Service to list information within the `Home.razor`, we will then refactor a bit to help with reusability.

### Initial Version of Home.Razor

Replace the contents of your existing Home.razor with the below code

```` csharp
@page "/"

@inject IWorkOrderService WorkOrders

<h1>Assigned work orders</h1>
<p class="muted">Your upcoming field visits</p>

@foreach (var order in orders)
{
    <article class="card">
        <div class="row"><strong>@order.Number</strong><span class="pill">@order.Status</span></div>
        <h3>@order.CustomerName</h3>
        <p>@order.Summary</p>
        <p class="muted">@order.ScheduledFor.ToLocalTime().ToString("MMM d, yyyy • h:mm tt")</p>
        <a class="btn" href="@($"/orders/{order.Id}")">View details</a>
    </article>
}

@code {
    private IReadOnlyList<WorkOrder> orders = [];

    protected override async Task OnInitializedAsync() => orders = await WorkOrders.GetAllAsync();
}
````

If you run the application at this point you should see orders displayed, clicking al ink will be a "not found"

### Improve Reusability with a Component

We may want to make this easier to use, so to do this, lets create a shared Component called `WorkOrder.razor` that can be reused.

- Create a folder under `Components` called `Shared`
- Add a new `Razor Component` named `WorkOrder.razor`
- Replace the default content with the below

```` csharp
<article class="card">
    <div class="row"><strong>@Order.Number</strong><span class="pill">@Order.Status</span></div>
    <h3>@Order.CustomerName</h3>
    <p>@Order.Summary</p>
    <p class="muted">@Order.ScheduledFor.ToLocalTime().ToString("MMM d, yyyy • h:mm tt")</p>
    <a class="btn" href="@($"/orders/{Order.Id}")">View details</a>
</article>

@code {
    [Parameter, EditorRequired] 
    public WorkOrder Order { get; set; } = default!;
}
````

Now, Update the code within the foreach loop on `Home.razor` to be

```` csharp
@foreach (var order in orders)
{
    <WorkOrderCard Order="order" />
}
````

You will also need to add `@Using FieldOps.Mobile.Components.Shared` to the `_Imports.razor`

## Create A Detail View

Create a new `Razor Component` titled `WorkOrderDetails.razor` within the `/pages` folder and put the following content into this file

```` csharp
@page "/orders/{Id:guid}"

@inject IWorkOrderService WorkOrders

<h1>Work order details</h1>

@if (order is null)
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
    
    protected override async Task OnParametersSetAsync() => order = await WorkOrders.GetByIdAsync(Id);
}
````

## Validation & What's Next

- Project should run successfully and you should be able to navigate to detailed entries
- A "not found" message, may be temporarily shown, discuss why/how/what to do to fix
- Understand the proper lifecycle of progression.
