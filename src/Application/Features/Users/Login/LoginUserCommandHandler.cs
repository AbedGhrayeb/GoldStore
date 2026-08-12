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
        // the user's own TenantId establishes the tenant for the session.
        User? user = await context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

        if (user is null)
        {
            return ApplicationErrors.LoginFailed;
        }

        bool verified = passwordHasher.Verify(command.Password, user.PasswordHash);

        if (!verified)
        {
            return ApplicationErrors.LoginFailed;
        }

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

        return IsSignInAllowed(tenant, timeProvider.GetUtcNow())
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
