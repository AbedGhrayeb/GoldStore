using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Users.Register;

internal sealed class RegisterUserCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher)
    : ICommandHandler<RegisterUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        if (await context.Users.AnyAsync(u => u.Email == command.Email, cancellationToken))
        {
            return UserErrors.EmailNotUnique;
        }

        Result<User> user = User.Create(Guid.CreateVersion7(), command.Email, command.FirstName, command.LastName, passwordHasher.Hash(command.Password), command.Role.ToString());
        if (user.IsError)
        {
            return user.Errors;
        }
        context.Users.Add(user.Value);
        await context.SaveChangesAsync(cancellationToken);

        return user.Value.Id;
    }
}
