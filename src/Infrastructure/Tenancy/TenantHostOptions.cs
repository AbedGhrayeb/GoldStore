namespace Infrastructure.Tenancy;

/// <summary>
/// Host-based tenant resolution settings (plan Phase 2). The canonical tenant host is
/// <c>{tenant-key}.{BaseDomain}</c>; the public login/onboarding host is <c>PublicHost</c>.
/// Hostname verification is opt-in so single-host local development keeps working; when it
/// is enabled, authenticated requests are verified against the tenant key in their claims
/// and public-host requests are redirected to the tenant's canonical subdomain.
/// </summary>
public sealed class TenantHostOptions
{
    public const string SectionName = "Tenancy";

    /// <summary>The host serving public login and onboarding, e.g. <c>goldstore.app</c>.</summary>
    public string PublicHost { get; set; } = "goldstore.app";

    /// <summary>The parent domain tenant subdomains live under, e.g. <c>goldstore.app</c>.</summary>
    public string BaseDomain { get; set; } = "goldstore.app";

    /// <summary>The scheme used to build canonical tenant URLs.</summary>
    public string Scheme { get; set; } = "https";

    /// <summary>
    /// True to verify the request hostname against the claimed tenant key and redirect
    /// public-host requests to the tenant subdomain. Disabled during local development.
    /// </summary>
    public bool RequireHostnameVerification { get; set; }

    /// <summary>
    /// When set, the authentication cookie is scoped to this parent domain (e.g.
    /// <c>.goldstore.app</c>) so a session created on the public host works on every
    /// tenant subdomain. Left null/empty for single-host development.
    /// </summary>
    public string? CookieDomain { get; set; }

    /// <summary>
    /// Explicit path prefixes exempt from tenant resolution. Non-endpoint paths, health
    /// checks, and authentication flows are listed here rather than excluded by convention.
    /// Endpoints may also opt out explicitly with <c>[HostOnly]</c> or <c>[AllowAnonymous]</c>.
    /// </summary>
    public string[] ExemptPaths { get; set; } =
    [
        "/health",
        "/Account/Logout",
        "/Account/AccessDenied",
        "/js",
        "/css",
        "/lib",
        "/images"
    ];
}
