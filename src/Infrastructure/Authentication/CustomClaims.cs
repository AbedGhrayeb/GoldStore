// <copyright file="CustomClaims.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Infrastructure.Authentication;

/// <summary>
/// Custom claim types carried by the authentication cookie and the access token. The
/// tenant claims bind every authenticated session to exactly one tenant; the server-side
/// tenant context is resolved from these claims only. The tenant middleware (WebUI) reads
/// the same claim names, so the constants are public.
/// </summary>
public static class CustomClaims
{
    public const string TenantId = "tenant_id";
    public const string TenantKey = "tenant_key";

    /// <summary>One claim per granted permission key (plan Phase 4 item 3).</summary>
    public const string Permission = "permission";

    /// <summary>One claim per tenant-enabled feature key (bare key, e.g. `catalog`).</summary>
    public const string Feature = "feature";

    /// <summary>The user's current security stamp (session/token version).</summary>
    public const string SecurityStamp = "security_stamp";

    /// <summary>Marker claim present only on host (PlatformUser) identities.</summary>
    public const string IsHost = "is_host";
}
