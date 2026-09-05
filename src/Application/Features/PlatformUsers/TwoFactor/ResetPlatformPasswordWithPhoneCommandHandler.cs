// <copyright file="ResetPlatformPasswordWithPhoneCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Phone;
using Application.Common.Errors;
using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.PlatformUsers.TwoFactor;

internal sealed class ResetPlatformPasswordWithPhoneCommandHandler(
    IApplicationDbContext context,
    IPhoneVerifier phoneVerifier,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider) : ICommandHandler<ResetPlatformPasswordWithPhoneCommand, Guid>
{
    public async Task<Result<Guid>> Handle(ResetPlatformPasswordWithPhoneCommand command, CancellationToken cancellationToken)
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
        PlatformUser? user = await context.PlatformUsers.SingleOrDefaultAsync(u => u.Email == normalized || u.PhoneNumber == normalized, cancellationToken);
        if (user is null)
        {
            return ApplicationErrors.InvalidPhoneVerification;
        }

        if (!user.PhoneNumberVerified || !string.Equals(user.PhoneNumber, verified.E164, StringComparison.Ordinal))
        {
            return ApplicationErrors.PhoneNotVerified;
        }

        user.ChangePassword(passwordHasher.Hash(command.NewPassword));
        user.ResetLoginAttempts();

        await context.SaveChangesAsync(cancellationToken);
        return user.Id;
    }
}
