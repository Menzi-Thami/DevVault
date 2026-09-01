using System.Net;
using System.Text.Json;
using DevVault.Application.Common.Exceptions;
using DevVault.Domain.Common;

namespace DevVault.API.Middleware;

/// <summary>
/// Translates typed application exceptions into HTTP responses in one place,
/// so handlers/controllers throw meaningfully and never swallow errors.
/// </summary>
public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var status = ex switch
            {
                NotFoundException => HttpStatusCode.NotFound,
                DomainException => HttpStatusCode.BadRequest,
                ArgumentException => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.InternalServerError
            };

            if (status == HttpStatusCode.InternalServerError)
                logger.LogError(ex, "Unhandled exception");

            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                status = (int)status,
                title = status == HttpStatusCode.InternalServerError
                    ? "An unexpected error occurred."
                    : ex.Message
            }));
        }
    }
}
