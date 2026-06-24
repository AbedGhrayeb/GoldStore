using Application.Abstractions.Messaging;

namespace Application.Users.GetAllUsers;

public sealed record GetUsersQuery : IQuery<List<UserResponse>>;
