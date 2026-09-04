using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Phone;
using Application.Common.Errors;
using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.PlatformUsers.TwoFactor;

internal sealed class VerifyPlatformPhoneTwoFactorCommandHandler(
    IApplicationDbContext context,
    IPhoneVerifier phoneVerifier,
    ITwoFactorTicketService ticketService,
    TimeProvider timeProvider) : ICommandHandler<VerifyPlatformPhoneTwoFactorCommand, Guid>
{
    public async Task<Result<Guid>> Handle(VerifyPlatformPhoneTwoFactorCommand command, CancellationToken cancellationToken)
    {
        if (!ticketService.TryValidateTicket(command.TempTicket, out Guid userId, out _, out bool isHost) || !isHost)
        {
            return ApplicationErrors.InvalidTwoFactorTicket;
        }

        PlatformUser? user = await context.PlatformUsers.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
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
        { verified = await phoneVerifier.VerifyAsync(command.IdToken, cancellationToken); }
        catch (Exception ex)
        {
            user.RecordFailedLoginAttempt(PlatformUser.MaxFailedLoginAttempts, PlatformUser.LockoutDuration, utcNow);
            await context.SaveChangesAsync(cancellationToken);
            return ApplicationErrors.InvalidPhoneVerification;
        }

        if (!string.Equals(verified.E164, user.PhoneNumber, StringComparison.Ordinal))
        {
            user.RecordFailedLoginAttempt(PlatformUser.MaxFailedLoginAttempts, PlatformUser.LockoutDuration, utcNow);
            await context.SaveChangesAsync(cancellationToken);
            return ApplicationErrors.InvalidPhoneVerification;
        }

        if (!user.IsActive)
        {
            return ApplicationErrors.UserDisabled;
        }

        user.ResetLoginAttempts();
        await context.SaveChangesAsync(cancellationToken);
        return user.Id;
    }
}

internal sealed class VerifyPlatformWithRecoveryCodeCommandHandler(
    IApplicationDbContext context,
    ITwoFactorTicketService ticketService,
    IPhoneRecoveryCodeService recoveryCodeService,
    TimeProvider timeProvider) : ICommandHandler<VerifyPlatformWithRecoveryCodeCommand, Guid>
{
    public async Task<Result<Guid>> Handle(VerifyPlatformWithRecoveryCodeCommand command, CancellationToken cancellationToken)
    {
        if (!ticketService.TryValidateTicket(command.TempTicket, out Guid userId, out _, out bool isHost) || !isHost)
        {
            return ApplicationErrors.InvalidTwoFactorTicket;
        }

        PlatformUser? user = await context.PlatformUsers.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
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
        PlatformRecoveryCode? code = await context.PlatformRecoveryCodes.SingleOrDefaultAsync(c => c.PlatformUserId == user.Id && c.CodeHash == hash, cancellationToken);
        if (code is null || code.IsUsed)
        {
            user.RecordFailedLoginAttempt(PlatformUser.MaxFailedLoginAttempts, PlatformUser.LockoutDuration, utcNow);
            await context.SaveChangesAsync(cancellationToken);
            return ApplicationErrors.RecoveryCodeInvalid;
        }

        code.MarkUsed(utcNow);
        user.ResetLoginAttempts();
        await context.SaveChangesAsync(cancellationToken);
        return user.Id;
    }
}
