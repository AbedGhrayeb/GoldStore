// <copyright file="ApiRoutes.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace WebUI.Endpoints;

/// <summary>
/// Route prefixes for the tenant and host API surfaces (plan Phase 7a). Tenant
/// endpoints live under <c>api/v1</c> and authenticate with the JWT bearer scheme
/// (see the <c>/api</c> forward selector in Infrastructure). Host endpoints live under
/// <c>host/api/v1</c> and authenticate with the dedicated PlatformUser cookie scheme,
/// staying on the <c>/host</c> forward selector. Endpoints never accept a tenant id.
/// </summary>
public static class ApiRoutes
{
    /// <summary>The current API version. Bump for breaking changes and keep old groups.</summary>
    public const string Version = "v1";

    /// <summary>Prefix for ordinary tenant endpoints.</summary>
    public const string Tenant = $"api/{Version}";

    /// <summary>Prefix for host administration endpoints.</summary>
    public const string Host = $"host/api/{Version}";
}
