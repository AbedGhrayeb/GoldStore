// <copyright file="SecurityStampValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Security.Claims;
using Application.Abstractions.Data;
using Domain.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Authentication;

/// <summary>
/// Validates the security stamp claim on cookie sessions (plan Phase 4 item 3). The stamp
/// is checked against the database at most once every <see cref="ValidationInterval"/>, so
/// disabling a user or rotating their stamp revokes their cookie sessions without a
/// database hit on every request.
/// </summary>
internal sealed class SecurityStampValidator(IApplicationDbContext context, TimeProvider timeProvider)
{
    private static readonly TimeSpan ValidationInterval = TimeSpan.FromMinutes(30);

    public async Task ValidateAsync(CookieValidatePrincipalContext validationContext)
    {
        ClaimsPrincipal principal = validationContext.Principal;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        string? stamp = principal.FindFirstValue(CustomClaims.SecurityStamp);
        if (stamp is null || !Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId))
        {
            await RejectPrincipalAsync(validationContext);
            return;
        }

        DateTimeOffset? issuedUtc = validationContext.Properties?.IssuedUtc;
        if (issuedUtc is not null && timeProvider.GetUtcNow() < issuedUtc.Value.Add(ValidationInterval))
        {
            return;
        }

        // Cookie validation can run before tenant resolution; the user lookup selects the
        // identity directly and bypasses the tenant query filter.
        User? user = await context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userId, CancellationToken.None);

        if (user is null
            || !user.IsActive
            || !string.Equals(user.SecurityStamp, stamp, StringComparison.Ordinal))
        {
            await RejectPrincipalAsync(validationContext);
            return;
        }

        // Stamp re-verified: refresh the ticket so the next re-check happens later.
        validationContext.ShouldRenew = true;
    }

    private static async Task RejectPrincipalAsync(CookieValidatePrincipalContext validationContext)
    {
        validationContext.RejectPrincipal();
        await validationContext.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
