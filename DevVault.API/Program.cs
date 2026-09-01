using DevVault.API.Middleware;
using DevVault.Application;
using DevVault.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Each layer owns its own registration (composition root).
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Typed exceptions -> HTTP status codes, before anything else in the pipeline.
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Enforce HTTPS at the client via HSTS outside development (dev/tests stay on
    // plain HTTP). Pairs with UseHttpsRedirection below.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Exposed so the integration/functional test host can reference the entry point.
public partial class Program { }
