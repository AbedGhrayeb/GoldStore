// <copyright file="VerifyPhoneWithRecoveryCodeCommandHandler.cs" company="PlaceholderCompany">
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

internal sealed class VerifyPhoneWithRecoveryCodeCommandHandler(
    IApplicationDbContext context,
    ITwoFactorTicketService ticketService,
    IPhoneRecoveryCodeService recoveryCodeService,
    TimeProvider timeProvider) : ICommandHandler<VerifyPhoneWithRecoveryCodeCommand, Guid>
{
    public async Task<Result<Guid>> Handle(VerifyPhoneWithRecoveryCodeCommand command, CancellationToken cancellationToken)
    {
        if (!ticketService.TryValidateTicket(command.TempTicket, out Guid userId, out _, out bool isHost) || isHost)
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

        DateTimeOffset utcNow = timeProvider.GetUtcNow();
        if (user.IsLockedOut(utcNow))
        {
            return ApplicationErrors.AccountLocked;
        }

        if (!user.IsActive)
        {
            return ApplicationErrors.UserDisabled;
        }

        string hash = recoveryCodeService.Hash(command.RecoveryCode);

        UserRecoveryCode? code = await context.UserRecoveryCodes
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(c => c.UserId == user.Id && c.CodeHash == hash, cancellationToken);

        if (code is null || code.IsUsed)
        {
            user.RecordFailedLoginAttempt(User.MaxFailedLoginAttempts, User.LockoutDuration, utcNow);
            await context.SaveChangesAsync(cancellationToken);
            return ApplicationErrors.RecoveryCodeInvalid;
        }

        code.MarkUsed(utcNow);
        user.ResetLoginAttempts();
        await context.SaveChangesAsync(cancellationToken);

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
