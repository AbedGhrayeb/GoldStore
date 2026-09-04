using System.Security.Claims;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Domain.Tenants;
using Domain.Users;
using Infrastructure.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Authentication;

internal sealed class CookieAuthSessionManager(
    IApplicationDbContext context,
    IHttpContextAccessor httpContextAccessor,
    PermissionProvider permissionProvider) : IAuthSessionManager
{
    public async Task SignInAsync(string username, bool rememberMe, CancellationToken cancellationToken)
    {
        // Sign-in runs before a tenant context exists, so the user lookup bypasses
        // the tenant query filter; the user's own TenantId binds the session to
        // exactly one tenant.
        User user = await context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == username, cancellationToken) ?? throw new NullReferenceException();

        Tenant tenant = await context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == user.TenantId, cancellationToken) ?? throw new NullReferenceException();

        UserAuthorizationInfo authorization = await permissionProvider.GetForUserAsync(user.Id, user.TenantId, cancellationToken);

        IReadOnlyList<string> enabledFeatures = await context.TenantSettings
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(settings => settings.TenantId == user.TenantId)
            .Select(settings => settings.EnabledFeatures)
            .SingleOrDefaultAsync(cancellationToken) ?? [];

        IEnumerable<Claim> claims = BuildClaims(user, tenant, authorization, enabledFeatures);
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var props = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe
                ? DateTimeOffset.UtcNow.AddDays(14)
                : DateTimeOffset.UtcNow.AddHours(8),
            AllowRefresh = true
        };
        HttpContext _httpContext = httpContextAccessor.HttpContext ?? throw new NullReferenceException();
        await _httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
    }

    public Task SignOutAsync() => httpContextAccessor.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    private static IEnumerable<Claim> BuildClaims(
        User user,
        Tenant tenant,
        UserAuthorizationInfo authorization,
        IReadOnlyList<string> enabledFeatures)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new(CustomClaims.TenantId, user.TenantId.ToString()),
            new(CustomClaims.TenantKey, tenant.Key),
            new(CustomClaims.SecurityStamp, user.SecurityStamp),
        };

        claims.AddRange(authorization.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(authorization.Permissions.Select(permission => new Claim(CustomClaims.Permission, permission)));
        claims.AddRange(enabledFeatures.Select(feature => new Claim(CustomClaims.Feature, feature)));

        return claims;
    }
}
