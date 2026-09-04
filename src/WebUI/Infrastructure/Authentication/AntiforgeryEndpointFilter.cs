using Microsoft.AspNetCore.Antiforgery;

namespace WebUI.Infrastructure.Authentication;

/// <summary>
/// Validates the antiforgery request token (XSRF-TOKEN cookie + X-XSRF-TOKEN header) on
/// host cookie-authenticated mutations. The Angular client attaches the header via its XSRF
/// interceptor; SameSite=Strict alone is not a sufficient CSRF control for an admin surface.
/// Host tokens are purposely not bound to a specific claims user — the host JWT already
/// authenticates — so a username mismatch between token creation (anonymous login request)
/// and validation (authenticated host PUT/PATCH) must not fail the request.
/// </summary>
internal sealed class AntiforgeryEndpointFilter(IAntiforgery antiforgery) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException ex) when (ex.Message.Contains("different claims-based user", StringComparison.OrdinalIgnoreCase))
        {
            // Host CSRF token was minted for a different principal (anonymous vs host).
            // The host JWT cookie already proves authentication; the pair of
            // antiforgery cookie + X-XSRF-TOKEN header still proves same-site origin.
        }
        return await next(context);
    }
}
