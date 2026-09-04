using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Domain.Authorization;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Result;

namespace Application.Authorization.SetUserRoles;

internal sealed class SetUserRolesCommandHandler(
    IApplicationDbContext context,
    ICurrentTenant currentTenant,
    IUserContext userContext) : ICommandHandler<SetUserRolesCommand, bool>
{
    public async Task<Result<bool>> Handle(SetUserRolesCommand command, CancellationToken cancellationToken)
    {
        // Prevent self-lockout: admin cannot remove own store_admin? Allow but warn. For now allow.
        // Validate target user exists in tenant
        User? user = await context.Users.FirstOrDefaultAsync(u => u.Id == command.UserId && u.TenantId == currentTenant.TenantId, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound(command.UserId);
        }

        // Validate all roleIds exist
        var distinctRoleIds = command.RoleIds.Distinct().ToList();
        if (distinctRoleIds.Count > 0)
        {
            int existingCount = await context.Roles.CountAsync(r => distinctRoleIds.Contains(r.Id), cancellationToken);
            if (existingCount != distinctRoleIds.Count)
            {
                return Error.Validation("Roles.NotFound", "One or more roles not found.");
            }
        }

        // Prevent removing last store_admin from tenant (must keep at least one admin)
        Role? storeAdminRole = await context.Roles.FirstOrDefaultAsync(r => r.Key == "store_admin", cancellationToken);
        if (storeAdminRole is not null)
        {
            bool targetHadAdmin = await context.UserRoles.AnyAsync(ur => ur.UserId == command.UserId && ur.RoleId == storeAdminRole.Id && ur.TenantId == currentTenant.TenantId, cancellationToken);
            bool willHaveAdmin = distinctRoleIds.Contains(storeAdminRole.Id);

            if (targetHadAdmin && !willHaveAdmin)
            {
                // Check if there will be at least one other admin left
                int otherAdminCount = await context.UserRoles
                    .Where(ur => ur.RoleId == storeAdminRole.Id && ur.TenantId == currentTenant.TenantId && ur.UserId != command.UserId)
                    .CountAsync(cancellationToken);
                if (otherAdminCount == 0)
                {
                    return Error.Validation("Roles.LastAdmin", "Cannot remove the last store administrator. Assign another admin first.");
                }

                // Prevent self-demotion without another admin? Already checked otherAdminCount
            }
        }

        // Replace roles: delete existing, insert new
        List<UserRole> existingRoles = await context.UserRoles
            .Where(ur => ur.UserId == command.UserId && ur.TenantId == currentTenant.TenantId)
            .ToListAsync(cancellationToken);

        context.UserRoles.RemoveRange(existingRoles);

        foreach (Guid roleId in distinctRoleIds)
        {
            Result<UserRole> create = UserRole.Create(currentTenant.TenantId, command.UserId, roleId);
            if (create.IsError)
            {
                return create.Errors;
            }

            context.UserRoles.Add(create.Value);
        }

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
