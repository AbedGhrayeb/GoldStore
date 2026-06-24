using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common.Errors;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.GetAllUsers;

internal sealed class GetUsersQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetUsersQuery, List<UserResponse>>
{
    public async Task<Result<List<UserResponse>>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
    

       List<UserResponse>? users = await context.Users
            .Select(u => new UserResponse
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email
            })
            .ToListAsync(cancellationToken);

        if (users is null || !users.Any())
        {
            return Result.Failure<List<UserResponse>>(ApplicationErrors.EmptyUsersList);
        }

        return users;
    }
}
