using System.Security.Claims;
using System.Text;
using Application.Abstractions.Authentication;
using Application.Features.Platform.Auth.PlatformAdminLogin;
using Domain.Tenants;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SharedKernel;
using SharedKernel.Result;

namespace Infrastructure.Authentication;

internal sealed class PlatformTokenProvider(IConfiguration configuration, IDateTimeProvider dateTimeProvider)
    : IPlatformTokenProvider
{
    public const string PlatformAdminRole = "PlatformAdmin";

    public Task<Result<PlatformAdminLoginResponse>> CreateAsync(PlatformAdmin platformAdmin, CancellationToken ct)
    {
        IConfigurationSection jwtSettings = configuration.GetSection("Jwt");

        DateTime expires = dateTimeProvider.UtcNow.AddMinutes(configuration.GetValue<int>("Jwt:ExpirationInMinutes"));
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, platformAdmin.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, platformAdmin.Email),
                new Claim(ClaimTypes.Role, PlatformAdminRole)
            ]),
            Expires = expires,
            SigningCredentials = credentials,
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["PlatformAudience"]
        };

        var handler = new JsonWebTokenHandler();

        var response = new PlatformAdminLoginResponse(handler.CreateToken(tokenDescriptor), expires);

        return Task.FromResult<Result<PlatformAdminLoginResponse>>(response);
    }
}
