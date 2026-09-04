using Application.Abstractions.Messaging;

namespace Application.Authorization.SetUserPermissions;

public sealed record SetUserPermissionsCommand(Guid UserId, List<string> PermissionKeys) : ICommand<bool>;
