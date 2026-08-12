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
    IPasswordHasher passwordHasher) : ICommandHandler<LoginPlatformUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(LoginPlatformUserCommand command, CancellationToken cancellationToken)
    {
        // PlatformUser is a host identity with no tenant; the table is global.
        PlatformUser? user = await context.PlatformUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

        if (user is null || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            return ApplicationErrors.LoginFailed;
        }

        // Disabled host identities must not sign in (plan Phase 4 item 6).
        if (!user.IsActive)
        {
            return ApplicationErrors.UserDisabled;
        }

        return user.Id;
    }
}
