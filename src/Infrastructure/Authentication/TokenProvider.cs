using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Features.Identity;
using Domain.Users;
using Domain.Users.RefreshToken;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SharedKernel;
using SharedKernel.Result;

namespace Infrastructure.Authentication;

internal sealed class TokenProvider(IConfiguration configuration, IDateTimeProvider dateTimeProvider, IApplicationDbContext dbContext) : ITokenProvider
{
    public async Task<Result<TokenResponse>> CreateAsync(User user, CancellationToken ct = default)
    {
        var jwtSettings = configuration.GetSection("Jwt");

        string secretKey = jwtSettings["Secret"]!;
        string Issuer = jwtSettings["Issuer"]!;
        string Audience = jwtSettings["Audience"]!;
        var expires = dateTimeProvider.UtcNow.AddMinutes(configuration.GetValue<int>("Jwt:ExpirationInMinutes"));
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            ]),
            Expires = expires,
            SigningCredentials = credentials,
            Issuer = Issuer,
            Audience = Audience
        };

        var handler = new JsonWebTokenHandler();

        string token = handler.CreateToken(tokenDescriptor);
        await dbContext.RefreshTokens.Where(rt => rt.UserId == user.Id)
            .ExecuteDeleteAsync(ct);

        var refreshTokenResult = RefreshToken.Create(Guid.CreateVersion7(), GenerateRefreshToken(), user.Id, DateTime.UtcNow.AddDays(7));
        if (refreshTokenResult.IsError)
        {
            return refreshTokenResult.Errors;
        }
        var refreshToken = refreshTokenResult.Value;
        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(ct);


        return new TokenResponse
        {
            AccessToken = token,
            RefreshToken = refreshToken.Token,
            ExpiresOnUtc = expires
        };
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!)),
            ValidateIssuer = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = configuration["Jwt:Audience"],
            ValidateLifetime = false, // Ignore token expiration
            ClockSkew = TimeSpan.Zero
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256Signature, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token.");
        }

        return principal;
    }
    private string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}
