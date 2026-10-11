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
