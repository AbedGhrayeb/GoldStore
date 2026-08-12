using System.Security.Claims;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Domain.Tenants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Authentication;

/// <summary>
/// Signs host administrators in on the dedicated <c>HostAuth</c> scheme (plan Phase 4
/// item 6). Host identities carry no tenant and are authorized only on host-only endpoints.
/// </summary>
internal sealed class PlatformAuthSessionManager(
    IApplicationDbContext context,
    IHttpContextAccessor httpContextAccessor) : IPlatformAuthSessionManager
{
    public async Task SignInAsync(string email, bool rememberMe, CancellationToken cancellationToken)
    {
        PlatformUser user = await context.PlatformUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken)
            ?? throw new NullReferenceException();

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(CustomClaims.IsHost, bool.TrueString),
        ], HostAuthDefaults.AuthenticationScheme);

        var props = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe
                ? DateTimeOffset.UtcNow.AddDays(14)
                : DateTimeOffset.UtcNow.AddHours(8),
            AllowRefresh = true
        };

        HttpContext httpContext = httpContextAccessor.HttpContext ?? throw new NullReferenceException();
        await httpContext.SignInAsync(HostAuthDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), props);
    }

    public Task SignOutAsync() => httpContextAccessor.HttpContext!.SignOutAsync(HostAuthDefaults.AuthenticationScheme);
}
