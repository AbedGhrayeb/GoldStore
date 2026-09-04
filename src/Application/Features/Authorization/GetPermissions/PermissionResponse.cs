namespace Application.Authorization.GetPermissions;

public sealed record PermissionResponse
{
    public Guid Id { get; init; }
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}
