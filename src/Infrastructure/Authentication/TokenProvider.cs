using System.Security.Claims;
using System.Text;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Domain.Authorization;
using Domain.Tenants;
using Domain.Users;
using Infrastructure.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SharedKernel;

namespace Infrastructure.Authentication;

/// <summary>
/// Creates short-lived access tokens (plan Phase 4 items 3-4). Claims are immutable for
/// the lifetime of the token: user id, tenant id and key, roles, permissions, and the
/// security stamp. Token creation runs during login, before any tenant context exists, so
/// the user/tenant/role lookups select the user's tenant explicitly and bypass the global
/// query filter — the same exception the login flow already uses.
/// </summary>
internal sealed class TokenProvider(
    IConfiguration configuration,
    IApplicationDbContext context,
    PermissionProvider permissionProvider,
    IDateTimeProvider dateTimeProvider) : ITokenProvider
{
    public async Task<AccessTokenResponse> CreateAccessTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        User user = await context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException($"Cannot issue a token for unknown user '{userId}'.");

        Tenant tenant = await context.Tenants
            .AsNoTracking()
            .SingleAsync(t => t.Id == user.TenantId, cancellationToken);

        UserAuthorizationInfo authorization = await permissionProvider
            .GetForUserAsync(userId, user.TenantId, cancellationToken);

        string secretKey = configuration["Jwt:Secret"]!;
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
            new(CustomClaims.TenantId, user.TenantId.ToString()),
            new(CustomClaims.TenantKey, tenant.Key),
            new(CustomClaims.SecurityStamp, user.SecurityStamp),
        ];

        foreach (string role in authorization.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (string permission in authorization.Permissions)
        {
            claims.Add(new Claim(CustomClaims.Permission, permission));
        }

        DateTime expiresAt = dateTimeProvider.UtcNow.AddMinutes(configuration.GetValue("Jwt:ExpirationInMinutes", defaultValue: 60));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            IssuedAt = dateTimeProvider.UtcNow,
            SigningCredentials = credentials,
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"]
        };

        string token = new JsonWebTokenHandler().CreateToken(tokenDescriptor);

        return new AccessTokenResponse(token, new DateTimeOffset(expiresAt, TimeSpan.Zero));
    }
}
