using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Users.Update;

internal sealed class UpdateUserCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher, IUserContext userContext)
    : ICommandHandler<UpdateUserCommand, bool>
{
    public async Task<Result<bool>> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        if (userContext.UserId != command.Id)
        {
            return UserErrors.Unauthorized();
        }
        ;
        User user = await context.Users.FirstOrDefaultAsync(u => u.Id == command.Id, cancellationToken);

        if (user is null)
        {
            return UserErrors.NotFound(command.Id);
        }
        user.Update(command.FirstName, command.LastName, string.IsNullOrEmpty(command.Password) ? null : passwordHasher.Hash(command.Password));
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
