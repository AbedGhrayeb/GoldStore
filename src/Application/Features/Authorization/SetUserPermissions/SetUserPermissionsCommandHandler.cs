using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Domain.Authorization;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Result;

namespace Application.Authorization.SetUserPermissions;

internal sealed class SetUserPermissionsCommandHandler(
    IApplicationDbContext context,
    ICurrentTenant currentTenant) : ICommandHandler<SetUserPermissionsCommand, bool>
{
    public async Task<Result<bool>> Handle(SetUserPermissionsCommand command, CancellationToken cancellationToken)
    {
        User? user = await context.Users.FirstOrDefaultAsync(u => u.Id == command.UserId && u.TenantId == currentTenant.TenantId, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound(command.UserId);
        }

        // Validate permission keys exist
        var distinctKeys = command.PermissionKeys.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        Dictionary<string, Guid> permissionMap = await context.Permissions.ToDictionaryAsync(p => p.Key, p => p.Id, cancellationToken);
        foreach (string? key in distinctKeys)
        {
            if (!permissionMap.ContainsKey(key))
            {
                return Error.Validation("Permissions.NotFound", $"Permission '{key}' not found.");
            }
        }

        // Prevent removing last admin's critical permission indirectly? Roles already protect, but direct permissions could remove last admin capability.
        // We enforce that tenant keeps at least one user with users.manage or store_admin role; skip for now and rely on role guard.

        // Replace direct permissions
        List<UserPermission> existing = await context.UserPermissions
            .Where(up => up.UserId == command.UserId && up.TenantId == currentTenant.TenantId)
            .ToListAsync(cancellationToken);
        context.UserPermissions.RemoveRange(existing);

        foreach (string? key in distinctKeys)
        {
            Guid permId = permissionMap[key];
            Result<UserPermission> create = UserPermission.Create(currentTenant.TenantId, command.UserId, permId);
            if (create.IsError)
            {
                return create.Errors;
            }

            context.UserPermissions.Add(create.Value);
        }

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
