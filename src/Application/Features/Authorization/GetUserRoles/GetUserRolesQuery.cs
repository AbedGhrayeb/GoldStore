using Application.Abstractions.Messaging;

namespace Application.Authorization.GetUserRoles;

public sealed record GetUserRolesQuery(Guid UserId) : IQuery<List<Guid>>;
