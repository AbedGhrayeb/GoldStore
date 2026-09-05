// <copyright file="ResetPasswordWithPhoneCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Phone;
using Application.Abstractions.Tenants;
using Application.Common.Errors;
using Domain.Tenants;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.Users.TwoFactor;

internal sealed class ResetPasswordWithPhoneCommandHandler(
    IApplicationDbContext context,
    IPhoneVerifier phoneVerifier,
    IPasswordHasher passwordHasher,
    ICurrentTenantSetter tenantSetter,
    TimeProvider timeProvider) : ICommandHandler<ResetPasswordWithPhoneCommand, Guid>
{
    public async Task<Result<Guid>> Handle(ResetPasswordWithPhoneCommand command, CancellationToken cancellationToken)
    {
        VerifiedPhone verified;
        try
        {
            verified = await phoneVerifier.VerifyAsync(command.IdToken, cancellationToken);
        }
        catch (Exception ex)
        {
            return ApplicationErrors.InvalidPhoneVerification;
        }

        string normalized = command.EmailOrPhone.Trim();
        User? user = await context.Users
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(u => u.Email == normalized || u.PhoneNumber == normalized, cancellationToken);

        if (user is null)
        {
            // Do not leak; still return generic failure but we need phone match check
            return ApplicationErrors.InvalidPhoneVerification;
        }

        if (!user.PhoneNumberVerified || !string.Equals(user.PhoneNumber, verified.E164, StringComparison.Ordinal))
        {
            return ApplicationErrors.PhoneNotVerified;
        }

        Tenant tenant = await context.Tenants.AsNoTracking().SingleAsync(t => t.Id == user.TenantId, cancellationToken);
        tenantSetter.Set(user.TenantId, tenant.Key);

        // Set new password + rotate stamp + clear lockout
        string hash = passwordHasher.Hash(command.NewPassword);
        user.Update(user.FirstName, user.LastName, hash, user.PhoneNumber, user.WhatsappNumber);
        user.RegenerateSecurityStamp();
        user.ResetLoginAttempts();

        // Revoke refresh tokens for this user
        List<RefreshToken> tokens = await context.RefreshTokens
            .IgnoreQueryFilters()
            .Where(t => t.UserId == user.Id && t.IsUsable)
            .ToListAsync(cancellationToken);
        DateTimeOffset utcNow = timeProvider.GetUtcNow();
        foreach (RefreshToken t in tokens)
        {
            t.Revoke(utcNow);
        }

        await context.SaveChangesAsync(cancellationToken);
        return user.Id;
    }
}
