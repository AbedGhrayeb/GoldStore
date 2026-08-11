namespace Infrastructure.Authentication;

/// <summary>
/// Custom claim types carried by the authentication cookie. The tenant claims bind
/// every authenticated session to exactly one tenant; the server-side tenant context
/// is resolved from these claims only.
/// </summary>
internal static class CustomClaims
{
    public const string TenantId = "tenant_id";
    public const string TenantKey = "tenant_key";
}
