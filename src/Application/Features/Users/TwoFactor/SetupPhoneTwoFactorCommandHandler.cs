// <copyright file="SetupPhoneTwoFactorCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

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

internal sealed class SetupPhoneTwoFactorCommandHandler(
    IApplicationDbContext context,
    IPhoneVerifier phoneVerifier,
    IPhoneRecoveryCodeService recoveryCodeService,
    ICurrentTenantSetter tenantSetter,
    TimeProvider timeProvider) : ICommandHandler<SetupPhoneTwoFactorCommand, SetupPhoneTwoFactorResult>
{
    public async Task<Result<SetupPhoneTwoFactorResult>> Handle(SetupPhoneTwoFactorCommand command, CancellationToken cancellationToken)
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

        User? user = await context.Users
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return ApplicationErrors.LoginFailed;
        }

        // Normalize phone: verify matches stored if already verified
        DateTimeOffset utcNow = timeProvider.GetUtcNow();

        Tenant tenant = await context.Tenants.AsNoTracking().SingleAsync(t => t.Id == user.TenantId, cancellationToken);
        tenantSetter.Set(user.TenantId, tenant.Key);

        user.EnableTwoFactor(verified.E164, utcNow);

        // Generate recovery codes
        IReadOnlyList<string> plainCodes = recoveryCodeService.GeneratePlainCodes();
        List<UserRecoveryCode> existing = await context.UserRecoveryCodes
            .IgnoreQueryFilters()
            .Where(c => c.UserId == user.Id)
            .ToListAsync(cancellationToken);

        context.UserRecoveryCodes.RemoveRange(existing);

        foreach (string code in plainCodes)
        {
            string hash = recoveryCodeService.Hash(code);
            context.UserRecoveryCodes.Add(UserRecoveryCode.Create(user.TenantId, user.Id, hash));
        }

        await context.SaveChangesAsync(cancellationToken);

        return new SetupPhoneTwoFactorResult(verified.E164, plainCodes);
    }
}
