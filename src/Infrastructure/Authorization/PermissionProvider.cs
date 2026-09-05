// <copyright file="PermissionProvider.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Authorization;

/// <summary>
/// Loads the roles and permissions granted to a user inside a specific tenant (plan
/// Phase 4). Used when building cookie and access-token claims during login, when no
/// tenant context exists yet, so lookups explicitly select the user's tenant and bypass
/// the global query filter — the same exception the login flow already uses.
/// </summary>
public sealed record UserAuthorizationInfo(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);

internal sealed class PermissionProvider(IApplicationDbContext context)
{
    public async Task<UserAuthorizationInfo> GetForUserAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        List<string> roles = await (from userRole in context.UserRoles.IgnoreQueryFilters()
                                    where userRole.UserId == userId && userRole.TenantId == tenantId
                                    join role in context.Roles on userRole.RoleId equals role.Id
                                    select role.Key).Distinct().ToListAsync(cancellationToken);

        List<string> rolePermissions = await (from userRole in context.UserRoles.IgnoreQueryFilters()
                                              where userRole.UserId == userId && userRole.TenantId == tenantId
                                              join rolePermission in context.RolePermissions on userRole.RoleId equals rolePermission.RoleId
                                              join permission in context.Permissions on rolePermission.PermissionId equals permission.Id
                                              select permission.Key).Distinct().ToListAsync(cancellationToken);

        List<string> directPermissions = await (from userPerm in context.UserPermissions.IgnoreQueryFilters()
                                                where userPerm.UserId == userId && userPerm.TenantId == tenantId
                                                join permission in context.Permissions on userPerm.PermissionId equals permission.Id
                                                select permission.Key).Distinct().ToListAsync(cancellationToken);

        var permissions = rolePermissions.Concat(directPermissions).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        return new UserAuthorizationInfo(roles, permissions);
    }
}
