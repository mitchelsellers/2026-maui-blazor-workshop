using FieldOps.Api;
using FieldOps.Core.Models;

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
api.MapPut("/{id:guid}", (Guid id, UpdateWorkOrderRequest request, WorkOrderStore store) =>
{
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(request.Summary) || request.Summary.Trim().Length is < 5 or > 200)
        errors[nameof(request.Summary)] = ["Summary must contain between 5 and 200 characters."];
    if (!Enum.IsDefined(request.Status)) errors[nameof(request.Status)] = ["Invalid status."];
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    return store.Update(id, request) is { } order ? Results.Ok(order) : Results.NotFound();
});
app.Run();
