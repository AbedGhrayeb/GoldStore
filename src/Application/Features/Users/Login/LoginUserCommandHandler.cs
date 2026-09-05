// <copyright file="LoginUserCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common.Errors;
using Domain.Tenants;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Users.Login;

internal sealed class LoginUserCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider) : ICommandHandler<LoginUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        // Login runs before any tenant context exists. Email addresses are unique
        // system-wide, so the lookup deliberately bypasses the tenant query filter;
        // the user's own TenantId establishes the tenant for the session. The row is
        // tracked so failed-attempt and lockout state can be persisted.
        User? user = await context.Users
            .IgnoreQueryFilters()
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

        bool verified = passwordHasher.Verify(command.Password, user.PasswordHash);

        if (!verified)
        {
            user.RecordFailedLoginAttempt(User.MaxFailedLoginAttempts, User.LockoutDuration, utcNow);
            await context.SaveChangesAsync(cancellationToken);

            return ApplicationErrors.LoginFailed;
        }

        user.ResetLoginAttempts();
        await context.SaveChangesAsync(cancellationToken);

        // The password check runs first so an unauthenticated caller cannot tell
        // whether the account is disabled (plan Phase 4 item 2).
        if (!user.IsActive)
        {
            return ApplicationErrors.UserDisabled;
        }

        Tenant? tenant = await context.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == user.TenantId, cancellationToken);

        if (tenant is null)
        {
            return ApplicationErrors.LoginFailed;
        }

        return IsSignInAllowed(tenant, utcNow)
            ? user.Id
            : ApplicationErrors.TenantAccessDenied;
    }

    private static bool IsSignInAllowed(Tenant tenant, DateTimeOffset utcNow) => tenant.Status switch
    {
        TenantStatus.Active => true,
        TenantStatus.Trial => tenant.TrialEndsAtUtc is null || tenant.TrialEndsAtUtc > utcNow,
        TenantStatus.Cancelled => tenant.CancellationReadOnlyUntilUtc is not null && tenant.CancellationReadOnlyUntilUtc > utcNow,
        _ => false,
    };
}
