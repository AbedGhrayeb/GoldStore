using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.Delete;

internal sealed class DeleteUserCommandHandler(IApplicationDbContext context)
    : ICommandHandler<DeleteUserCommand, bool>
{
    public async Task<Result<bool>> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        User user = await context.Users.FirstOrDefaultAsync(u => u.Id == command.Id, cancellationToken);

        if (user is null)
        {
            return Result.Failure<bool>(UserErrors.NotFound(command.Id));
        }

        context.Users.Remove(user);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
