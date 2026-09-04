using Application.Abstractions.Messaging;

namespace Application.Authorization.GetPermissions;

public sealed record GetPermissionsQuery : IQuery<List<PermissionResponse>>;
