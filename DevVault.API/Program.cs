using System.Diagnostics;
using DevVault.API.ErrorHandling;
using DevVault.Application;
using DevVault.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// One error shape (RFC 9457 ProblemDetails) for typed exceptions and model-binding failures alike.
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
// Run in registration order; the first to return true wins.
builder.Services.AddExceptionHandler<KnownExceptionHandler>();
builder.Services.AddExceptionHandler<UnhandledExceptionHandler>();

// Each layer owns its own registration (composition root).
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Outermost, so every exception below it becomes ProblemDetails.
app.UseExceptionHandler();

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
