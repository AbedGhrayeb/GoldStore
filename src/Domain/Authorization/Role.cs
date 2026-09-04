using SharedKernel;
using SharedKernel.Result;

namespace Domain.Authorization;

/// <summary>
/// A global role template (plan Phase 0 item 9). Roles are shared definitions, not
/// tenant-owned; tenants assign roles to their own users. This keeps permission
/// administration centralized while user-role assignments stay tenant-scoped.
/// </summary>
public sealed class Role : Entity
{
    public string Key { get; private set; }

    public string Name { get; private set; }

    public ICollection<RolePermission> RolePermissions { get; private set; } = [];

    private Role()
    {
        Key = string.Empty;
        Name = string.Empty;
    }

    private Role(Guid id, string key, string name) : base(id)
    {
        Key = key;
        Name = name;
    }

    public static Result<Role> Create(string key, string name)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return AuthorizationErrors.KeyRequired;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return AuthorizationErrors.NameRequired;
        }

        return new Role(Guid.CreateVersion7(), key.Trim().ToLowerInvariant(), name.Trim());
    }

    public void AddPermission(Guid permissionId)
    {
        if (RolePermissions.Any(rolePermission => rolePermission.PermissionId == permissionId))
        {
            return;
        }

        RolePermissions.Add(new RolePermission(Guid.CreateVersion7(), Id, permissionId));
    }
}
