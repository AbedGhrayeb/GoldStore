using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.Update;

internal sealed class UpdateUserCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher)
    : ICommandHandler<UpdateUserCommand, bool>
{
    public async Task<Result<bool>> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        User user = await context.Users.FirstOrDefaultAsync(u => u.Id == command.Id, cancellationToken);

        if (user is null)
        {
            return Result.Failure<bool>(UserErrors.NotFound(command.Id));
        }

        user.FirstName = command.FirstName;
        user.LastName = command.LastName;

        if (!string.IsNullOrEmpty(command.Password))
        {
            user.PasswordHash = passwordHasher.Hash(command.Password);
        }

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
