namespace Infrastructure.Authentication;

/// <summary>
/// Custom claim types carried by the authentication cookie. The tenant claims bind
/// every authenticated session to exactly one tenant; the server-side tenant context
/// is resolved from these claims only. The tenant middleware (WebUI) reads the same
/// claim names, so the constants are public.
/// </summary>
public static class CustomClaims
{
    public const string TenantId = "tenant_id";
    public const string TenantKey = "tenant_key";
}
