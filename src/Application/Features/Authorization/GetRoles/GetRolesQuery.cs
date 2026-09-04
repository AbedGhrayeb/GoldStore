using Application.Abstractions.Messaging;

namespace Application.Authorization.GetRoles;

public sealed record GetRolesQuery : IQuery<List<RoleResponse>>;
