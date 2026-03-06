using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CloudPizza.Api.Infrastructure;

/// <summary>
/// Global exception handler for unhandled exceptions.
/// Keeps business flow exception-free while handling unexpected failures centrally.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        logger.LogError(exception,
            "Unhandled exception. TraceId: {TraceId}, Path: {Path}",
            traceId,
            httpContext.Request.Path);

        var problem = new ProblemDetails
        {
            Title = "An unexpected error occurred.",
            Detail = "An internal server error occurred. Use the trace id for support.",
            Status = StatusCodes.Status500InternalServerError,
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["traceId"] = traceId
            }
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}
