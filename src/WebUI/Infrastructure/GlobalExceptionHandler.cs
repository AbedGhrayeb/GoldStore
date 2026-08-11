using Infrastructure.Authentication;
using Infrastructure.Tenants;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Infrastructure;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails problemDetails = exception switch
        {
            TenantAccessViolationException => TenantProblemDetails.AccessViolation,
            CurrentTenantUnavailableException => TenantProblemDetails.MissingContext,
            UserContextUnavailableException => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.2",
                Title = "Authentication required",
                Detail = "This operation requires an authenticated user."
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
                Title = "Server failure"
            }
        };

        if (problemDetails.Status is >= 400 and < 500)
        {
            logger.LogWarning(exception, "Request was rejected: {ProblemTitle}", problemDetails.Title);
        }
        else
        {
            logger.LogError(exception, "Unhandled exception occurred");
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
