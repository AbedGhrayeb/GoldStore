// <copyright file="TwoFactorTicketService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Abstractions.Phone;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Phone;

internal sealed class TwoFactorTicketService(IConfiguration configuration) : ITwoFactorTicketService
{
    public string CreateTicket(Guid userId, Guid? tenantId, string email, bool isHost)
    {
        string secret = configuration["Jwt:Secret"]!;
        string issuer = configuration["Jwt:Issuer"]!;
        string audience = configuration["Jwt:Audience"]!;
        int minutes = configuration.GetValue("TwoFactor:TempTicketMinutes", 5);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("purpose", "2fa_pending"),
            new("isHost", isHost.ToString()),
        ];

        if (tenantId.HasValue)
        {
            claims.Add(new Claim("tid", tenantId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool TryValidateTicket(string ticket, out Guid userId, out Guid? tenantId, out bool isHost)
    {
        userId = Guid.Empty;
        tenantId = null;
        isHost = false;

        string secret = configuration["Jwt:Secret"]!;
        string issuer = configuration["Jwt:Issuer"]!;
        string audience = configuration["Jwt:Audience"]!;

        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        try
        {
            ClaimsPrincipal principal = handler.ValidateToken(ticket, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromMinutes(1),
            }, out SecurityToken validated);

            string? purpose = principal.FindFirst("purpose")?.Value;
            if (purpose != "2fa_pending")
            {
                return false;
            }

            string? sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(sub, out userId))
            {
                return false;
            }

            string? tid = principal.FindFirst("tid")?.Value;
            if (Guid.TryParse(tid, out Guid parsedTid))
            {
                tenantId = parsedTid;
            }

            isHost = bool.TryParse(principal.FindFirst("isHost")?.Value, out bool h) && h;

            return true;
        }
        catch
        {
            return false;
        }
    }
}
