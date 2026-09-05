// <copyright file="LoginPlatformUserCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common.Errors;
using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.PlatformUsers.Login;

internal sealed class LoginPlatformUserCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider) : ICommandHandler<LoginPlatformUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(LoginPlatformUserCommand command, CancellationToken cancellationToken)
    {
        // PlatformUser is a host identity with no tenant; the table is global. The row is
        // tracked so failed-attempt and lockout state can be persisted.
        PlatformUser? user = await context.PlatformUsers
            .SingleOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

        if (user is null)
        {
            return ApplicationErrors.LoginFailed;
        }

        DateTimeOffset utcNow = timeProvider.GetUtcNow();

        if (user.IsLockedOut(utcNow))
        {
            return ApplicationErrors.AccountLocked;
        }

        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            user.RecordFailedLoginAttempt(PlatformUser.MaxFailedLoginAttempts, PlatformUser.LockoutDuration, utcNow);
            await context.SaveChangesAsync(cancellationToken);

            return ApplicationErrors.LoginFailed;
        }

        user.ResetLoginAttempts();
        await context.SaveChangesAsync(cancellationToken);

        // Disabled host identities must not sign in (plan Phase 4 item 6).
        if (!user.IsActive)
        {
            return ApplicationErrors.UserDisabled;
        }

        return user.Id;
    }
}
