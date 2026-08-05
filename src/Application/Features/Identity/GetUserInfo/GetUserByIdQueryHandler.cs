using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Features.Identity.Dtos;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Result;

namespace Application.Features.Identity.GetUserInfo;

public sealed class GetUserByIdQueryHandler(ILogger<GetUserByIdQueryHandler> logger,
        IApplicationDbContext dbContext,
        IUserContext userContext)
    : IQueryHandler<GetUserInfoByIdQuery, UserDto>
{
    private readonly ILogger<GetUserByIdQueryHandler> _logger = logger;
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly IUserContext _userContext = userContext;

    public async Task<Result<UserDto>> Handle(GetUserInfoByIdQuery query, CancellationToken ct = default)
    {
        var userId = _userContext.UserId;
        if (userId == null)
        {
            return UserErrors.IdRequired;
        }
        var getUserByIdResult = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (getUserByIdResult == null)
        {
            logger.LogError("User with Id { UserId }{ErrorDetails}", userId, UserErrors.NotFound(userId!));

            return UserErrors.NotFound(userId);
        }
        return new UserDto(getUserByIdResult.Id, $"{getUserByIdResult.FirstName} {getUserByIdResult.LastName}", getUserByIdResult.Email, getUserByIdResult.Role);

    }
}
