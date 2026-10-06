using Asp.Versioning.ApiExplorer;
using DevVault.API.Authentication;
using DevVault.API.ErrorHandling;
using DevVault.API.Observability;
using DevVault.API.RateLimiting;
using DevVault.API.Versioning;
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

// Every log record carries the request's trace/span ids as a scope (the host default today;
// stated so the correlation contract doesn't hinge on a default).
builder.Logging.Configure(options => options.ActivityTrackingOptions =
    ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId | ActivityTrackingOptions.ParentId);

builder.Services.AddControllers();
// Routes are /api/v{version}/...; one OpenAPI document per version.
builder.Services.AddUrlSegmentApiVersioning();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// One error shape (RFC 9457 ProblemDetails) for typed exceptions and model-binding failures alike.
// traceId is the same value as the X-Trace-Id header and the logs' TraceId.
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = TraceCorrelation.CurrentTraceId(context.HttpContext));
// Run in registration order; the first to return true wins.
builder.Services.AddExceptionHandler<KnownExceptionHandler>();
builder.Services.AddExceptionHandler<UnhandledExceptionHandler>();

// Bearer tokens from the issuer in Authentication:Jwt; everything requires a signed-in user.
builder.Services.AddJwtAuthentication();

// Per-caller limits (user, else IP), stricter on create; 429 ProblemDetails + Retry-After.
builder.Services.AddApiRateLimiting();

// Traces, metrics and logs via OpenTelemetry; exported only where an endpoint is configured.
builder.Services.AddObservability(builder.Configuration);

// Each layer owns its own registration (composition root).
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

// First, so every response (errors and 401s included) tells the client which trace to quote.
app.UseTraceIdHeader();

// Outermost handler, so every exception below it becomes ProblemDetails.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
        options.AddVersionEndpoints(app.Services.GetRequiredService<IApiVersionDescriptionProvider>()));
}
else
{
    // Enforce HTTPS at the client via HSTS outside development (dev/tests stay on
    // plain HTTP). Pairs with UseHttpsRedirection below.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
// After authentication, so limits partition by the signed-in user rather than only by IP.
app.UseRateLimiter();

// Liveness has no dependency checks, so a database blip doesn't get every instance restarted.
// Probes carry no token, so both opt out of the authenticated fallback policy, and a busy
// client must never get the instance marked unhealthy, so both are exempt from rate limiting.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
    .AllowAnonymous()
    .DisableRateLimiting();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains(HealthCheckTags.Ready)
}).AllowAnonymous()
    .DisableRateLimiting();
app.MapControllers();

app.Run();

// Exposed so the integration/functional test host can reference the entry point.
public partial class Program { }
