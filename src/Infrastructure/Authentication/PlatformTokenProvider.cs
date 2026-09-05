// <copyright file="PlatformTokenProvider.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Security.Claims;
using System.Text;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SharedKernel;

namespace Infrastructure.Authentication;

/// <summary>
/// Issues short-lived JWTs for platform administrators. The token has no tenant claims, which
/// keeps host identities isolated from tenant endpoints at the authentication boundary.
/// </summary>
internal sealed class PlatformTokenProvider(
    IConfiguration configuration,
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider) : IPlatformTokenProvider
{
    public async Task<PlatformAccessTokenResponse> CreateAccessTokenAsync(
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        PlatformUser user = await context.PlatformUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(platformUser => platformUser.Id == platformUserId, cancellationToken)
            ?? throw new InvalidOperationException($"Cannot issue a token for unknown platform user '{platformUserId}'.");

        string secretKey = configuration["Jwt:Secret"]!;
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        DateTime expiresAt = dateTimeProvider.UtcNow.AddMinutes(
            configuration.GetValue("Jwt:ExpirationInMinutes", defaultValue: 60));

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
            new(ClaimTypes.Email, user.Email),
            new(CustomClaims.IsHost, bool.TrueString),
        ];

        if (user.TwoFactorEnabled && user.PhoneNumberVerified)
        {
            claims.Add(new Claim("amr", "mfa"));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            IssuedAt = dateTimeProvider.UtcNow,
            SigningCredentials = credentials,
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
        };

        string token = new JsonWebTokenHandler().CreateToken(tokenDescriptor);
        return new PlatformAccessTokenResponse(token, new DateTimeOffset(expiresAt, TimeSpan.Zero));
    }
}
