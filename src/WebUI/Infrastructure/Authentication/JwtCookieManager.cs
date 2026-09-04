using Application.Abstractions.Authentication;
using Infrastructure.Authentication;
using Infrastructure.Tenancy;
using Microsoft.Extensions.Options;

namespace WebUI.Infrastructure.Authentication;

/// <summary>
/// Stores browser JWTs in hardened HttpOnly cookies. The bearer handlers still accept an
/// Authorization header first, so this browser transport does not remove API-client support.
/// </summary>
internal sealed class JwtCookieManager(IOptions<TenantHostOptions> tenantHostOptions)
{
    public void SetTenantTokens(
        HttpContext context,
        AccessTokenResponse accessToken,
        RefreshTokenResponse refreshToken)
    {
        context.Response.Cookies.Append(
            JwtCookieDefaults.TenantAccessCookieName,
            accessToken.Value,
            CreateOptions(context, accessToken.ExpiresAtUtc, "/", includeTenantDomain: true));
        context.Response.Cookies.Append(
            JwtCookieDefaults.TenantRefreshCookieName,
            refreshToken.Value,
            CreateOptions(context, refreshToken.ExpiresAtUtc, "/api/v1/auth", includeTenantDomain: true));
    }

    public void SetHostToken(HttpContext context, PlatformAccessTokenResponse accessToken)
    {
        context.Response.Cookies.Append(
            JwtCookieDefaults.HostAccessCookieName,
            accessToken.Value,
            CreateOptions(context, accessToken.ExpiresAtUtc, "/host", includeTenantDomain: false));
    }

    public void DeleteTenantTokens(HttpContext context)
    {
        context.Response.Cookies.Delete(
            JwtCookieDefaults.TenantAccessCookieName,
            CreateOptions(context, null, "/", includeTenantDomain: true));
        context.Response.Cookies.Delete(
            JwtCookieDefaults.TenantRefreshCookieName,
            CreateOptions(context, null, "/api/v1/auth", includeTenantDomain: true));
    }

    public void DeleteHostToken(HttpContext context) =>
        context.Response.Cookies.Delete(
            JwtCookieDefaults.HostAccessCookieName,
            CreateOptions(context, null, "/host", includeTenantDomain: false));

    private CookieOptions CreateOptions(
        HttpContext context,
        DateTimeOffset? expiresAtUtc,
        string path,
        bool includeTenantDomain)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            Path = path,
            SameSite = SameSiteMode.Strict,
            Secure = context.Request.IsHttps,
        };

        if (expiresAtUtc.HasValue)
        {
            options.Expires = expiresAtUtc.Value;
        }

        string? cookieDomain = tenantHostOptions.Value.CookieDomain;
        if (includeTenantDomain && !string.IsNullOrWhiteSpace(cookieDomain))
        {
            options.Domain = cookieDomain;
        }

        return options;
    }
}
