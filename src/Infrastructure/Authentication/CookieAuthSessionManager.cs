using System.Security.Claims;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Domain.Tenants;
using Domain.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Authentication;

internal sealed class CookieAuthSessionManager(IApplicationDbContext context, IHttpContextAccessor httpContextAccessor) : IAuthSessionManager
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

        IEnumerable<Claim> claims = BuildClaims(user, tenant);
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

    //clams
    private static IEnumerable<Claim> BuildClaims(User user, Tenant tenant) =>
    [
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.Email),
        new(ClaimTypes.Email, user.Email),
        new(ClaimTypes.GivenName, user.FirstName),
        new(ClaimTypes.Surname, user.LastName),
        new(CustomClaims.TenantId, user.TenantId.ToString()),
        new(CustomClaims.TenantKey, tenant.Key),
    ];

}
