using Microsoft.AspNetCore.Mvc;

namespace WebUI.Infrastructure;

/// <summary>
/// Consistent RFC 9457 ProblemDetails for tenant resolution failures. Messages stay
/// generic so an unauthorized caller cannot tell whether another tenant exists.
/// </summary>
internal static class TenantProblemDetails
{
    private const string ForbiddenType = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.4";

    public static ProblemDetails MissingContext => Create(
        "tenant.missing",
        "Tenant context is missing",
        "This request could not be associated with a tenant.");

    public static ProblemDetails HostMismatch => Create(
        "tenant.host_mismatch",
        "Tenant host mismatch",
        "This request does not match the tenant's canonical host.");

    public static ProblemDetails AccessDenied => Create(
        "tenant.access_denied",
        "Tenant access denied",
        "This tenant is not currently able to use the application.");

    public static ProblemDetails AccessViolation => Create(
        "tenant.access_violation",
        "Tenant access violation",
        "This operation cannot be performed for the current tenant.");

    public static Task WriteAsync(HttpContext context, ProblemDetails details, CancellationToken cancellationToken = default)
    {
        context.Response.StatusCode = details.Status ?? StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";

        return context.Response.WriteAsJsonAsync(details, cancellationToken);
    }

    private static ProblemDetails Create(string code, string title, string detail) => new()
    {
        Status = StatusCodes.Status403Forbidden,
        Type = ForbiddenType,
        Title = title,
        Detail = detail,
        Extensions = { ["code"] = code }
    };
}
