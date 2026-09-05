// <copyright file="SetupPlatformPhoneTwoFactorCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Phone;
using Application.Common.Errors;
using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.PlatformUsers.TwoFactor;

internal sealed class SetupPlatformPhoneTwoFactorCommandHandler(
    IApplicationDbContext context,
    IPhoneVerifier phoneVerifier,
    IPhoneRecoveryCodeService recoveryCodeService,
    TimeProvider timeProvider) : ICommandHandler<SetupPlatformPhoneTwoFactorCommand, SetupPlatformPhoneTwoFactorResult>
{
    public async Task<Result<SetupPlatformPhoneTwoFactorResult>> Handle(SetupPlatformPhoneTwoFactorCommand command, CancellationToken cancellationToken)
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

        PlatformUser? user = await context.PlatformUsers.SingleOrDefaultAsync(u => u.Id == command.PlatformUserId, cancellationToken);
        if (user is null)
        {
            return ApplicationErrors.LoginFailed;
        }

        user.EnableTwoFactor(verified.E164, timeProvider.GetUtcNow());

        IReadOnlyList<string> plainCodes = recoveryCodeService.GeneratePlainCodes();
        List<PlatformRecoveryCode> existing = await context.PlatformRecoveryCodes.Where(c => c.PlatformUserId == user.Id).ToListAsync(cancellationToken);
        context.PlatformRecoveryCodes.RemoveRange(existing);
        foreach (string code in plainCodes)
        {
            context.PlatformRecoveryCodes.Add(PlatformRecoveryCode.Create(user.Id, recoveryCodeService.Hash(code)));
        }

        await context.SaveChangesAsync(cancellationToken);
        return new SetupPlatformPhoneTwoFactorResult(verified.E164, plainCodes);
    }
}
