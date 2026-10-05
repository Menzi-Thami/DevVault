using System.Diagnostics;
using DevVault.API.ErrorHandling;
using DevVault.Application;
using DevVault.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Fail at startup, in every environment (the defaults are Development-only), on missing
// registrations and scoped-into-singleton captures.
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

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
builder.Services.AddInfrastructure();

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

// Liveness has no dependency checks, so a database blip doesn't get every instance restarted.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains(HealthCheckTags.Ready)
});
app.MapControllers();

app.Run();

// Exposed so the integration/functional test host can reference the entry point.
public partial class Program { }
