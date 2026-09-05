// <copyright file="JwtCookieDefaults.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Infrastructure.Authentication;

/// <summary>Names of the HttpOnly cookies that carry browser JWTs.</summary>
public static class JwtCookieDefaults
{
    public const string TenantAccessCookieName = "GoldStore.AccessToken";

    public const string TenantRefreshCookieName = "GoldStore.RefreshToken";

    public const string HostAccessCookieName = "GoldStore.HostAccessToken";
}
