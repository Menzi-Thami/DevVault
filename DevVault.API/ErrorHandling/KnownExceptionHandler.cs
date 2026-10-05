using DevVault.Application.Common.Exceptions;
using DevVault.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DevVault.API.ErrorHandling;

/// <summary>
/// Maps the typed exceptions the application throws on purpose to ProblemDetails. These are the
/// client's mistakes, so they are not logged as errors. Anything else falls through to
/// <see cref="UnhandledExceptionHandler"/>.
/// </summary>
public sealed class KnownExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, code) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found", ErrorCodes.NotFound),
            DomainException => (StatusCodes.Status400BadRequest, "Invalid request", ErrorCodes.DomainRuleViolated),
            _ => (0, null, null)
        };

        if (code is null)
            return false;

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = exception.Message,
                Extensions = { ["code"] = code }
            }
        });
    }
}
