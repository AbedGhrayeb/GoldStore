using Application.Abstractions.Messaging;

namespace Application.Authorization.GetUserPermissions;

public sealed record GetUserPermissionsQuery(Guid UserId) : IQuery<List<string>>;
