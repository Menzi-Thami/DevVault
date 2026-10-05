using DevVault.Application.Common.Logging;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DevVault.API.ErrorHandling;

/// <summary>
/// Last in the chain: anything not mapped by <see cref="KnownExceptionHandler"/> is our bug.
/// It is logged once, here, and the client gets a generic 500 without internals. A request the
/// client abandoned is not an error, so it is neither logged as one nor answered with a body.
/// </summary>
public sealed class UnhandledExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<UnhandledExceptionHandler> logger) : IExceptionHandler
{
    public const int ClientClosedRequest = 499;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Request {Method} {Path} was aborted by the client",
                    LogValue.Safe(httpContext.Request.Method), LogValue.Safe(httpContext.Request.Path.Value));
            }
            httpContext.Response.StatusCode = ClientClosedRequest;
            return true;
        }

        logger.LogError(exception, "Unhandled exception for {Method} {Path}",
            LogValue.Safe(httpContext.Request.Method), LogValue.Safe(httpContext.Request.Path.Value));

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Extensions = { ["code"] = ErrorCodes.Unexpected }
            }
        });
    }
}
