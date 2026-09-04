using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Users.Register;

internal sealed class RegisterUserCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    ICurrentTenant currentTenant)
    : ICommandHandler<RegisterUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        // Email addresses are unique system-wide, so the uniqueness check must
        // bypass the tenant query filter.
        if (await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == command.Email, cancellationToken))
        {
            return UserErrors.EmailNotUnique;
        }

        Result<User> user = User.Create(Guid.CreateVersion7(), currentTenant.TenantId, command.Email, command.FirstName, command.LastName, passwordHasher.Hash(command.Password));
        if (user.IsError)
        {
            return user.Errors;
        }
        context.Users.Add(user.Value);
        await context.SaveChangesAsync(cancellationToken);

        return user.Value.Id;
    }
}
