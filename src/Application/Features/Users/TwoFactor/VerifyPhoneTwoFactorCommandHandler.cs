// <copyright file="VerifyPhoneTwoFactorCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Phone;
using Application.Common.Errors;
using Domain.Tenants;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.Users.TwoFactor;

internal sealed class VerifyPhoneTwoFactorCommandHandler(
    IApplicationDbContext context,
    IPhoneVerifier phoneVerifier,
    ITwoFactorTicketService ticketService,
    TimeProvider timeProvider) : ICommandHandler<VerifyPhoneTwoFactorCommand, Guid>
{
    public async Task<Result<Guid>> Handle(VerifyPhoneTwoFactorCommand command, CancellationToken cancellationToken)
    {
        if (!ticketService.TryValidateTicket(command.TempTicket, out Guid userId, out Guid? tenantId, out bool isHost) || isHost)
        {
            return ApplicationErrors.InvalidTwoFactorTicket;
        }

        User? user = await context.Users
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return ApplicationErrors.LoginFailed;
        }

        if (!user.TwoFactorEnabled || !user.PhoneNumberVerified)
        {
            return ApplicationErrors.TwoFactorNotEnabled;
        }

        DateTimeOffset utcNow = timeProvider.GetUtcNow();
        if (user.IsLockedOut(utcNow))
        {
            return ApplicationErrors.AccountLocked;
        }

        VerifiedPhone verified;
        try
        {
            verified = await phoneVerifier.VerifyAsync(command.IdToken, cancellationToken);
        }
        catch (Exception ex)
        {
            user.RecordFailedLoginAttempt(User.MaxFailedLoginAttempts, User.LockoutDuration, utcNow);
            await context.SaveChangesAsync(cancellationToken);
            return ApplicationErrors.InvalidPhoneVerification;
        }

        if (!string.Equals(verified.E164, user.PhoneNumber, StringComparison.Ordinal))
        {
            user.RecordFailedLoginAttempt(User.MaxFailedLoginAttempts, User.LockoutDuration, utcNow);
            await context.SaveChangesAsync(cancellationToken);
            return ApplicationErrors.InvalidPhoneVerification;
        }

        if (!user.IsActive)
        {
            return ApplicationErrors.UserDisabled;
        }

        user.ResetLoginAttempts();
        await context.SaveChangesAsync(cancellationToken);

        // Tenant operational check
        Tenant? tenant = await context.Tenants.AsNoTracking().SingleOrDefaultAsync(t => t.Id == user.TenantId, cancellationToken);
        if (tenant is null)
        {
            return ApplicationErrors.LoginFailed;
        }

        bool allowed = tenant.Status switch
        {
            Domain.Tenants.TenantStatus.Active => true,
            Domain.Tenants.TenantStatus.Trial => tenant.TrialEndsAtUtc is null || tenant.TrialEndsAtUtc > utcNow,
            Domain.Tenants.TenantStatus.Cancelled => tenant.CancellationReadOnlyUntilUtc is not null && tenant.CancellationReadOnlyUntilUtc > utcNow,
            _ => false,
        };
        if (!allowed)
        {
            return ApplicationErrors.TenantAccessDenied;
        }

        return user.Id;
    }
}
