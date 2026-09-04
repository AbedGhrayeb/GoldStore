using Application.Abstractions.Messaging;

namespace Application.Authorization.SetUserRoles;

public sealed record SetUserRolesCommand(Guid UserId, List<Guid> RoleIds) : ICommand<bool>;
