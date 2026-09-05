// <copyright file="GetUserPermissionsQueryHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Authorization.GetUserPermissions;

internal sealed class GetUserPermissionsQueryHandler(IApplicationDbContext context, ICurrentTenant currentTenant)
    : IQueryHandler<GetUserPermissionsQuery, List<string>>
{
    public async Task<Result<List<string>>> Handle(GetUserPermissionsQuery query, CancellationToken cancellationToken)
    {
        bool userExists = await context.Users.AnyAsync(u => u.Id == query.UserId && u.TenantId == currentTenant.TenantId, cancellationToken);
        if (!userExists)
        {
            return UserErrors.NotFound(query.UserId);
        }

        List<string> rolePermissions = await (from userRole in context.UserRoles
                                              where userRole.UserId == query.UserId && userRole.TenantId == currentTenant.TenantId
                                              join rolePermission in context.RolePermissions on userRole.RoleId equals rolePermission.RoleId
                                              join permission in context.Permissions on rolePermission.PermissionId equals permission.Id
                                              select permission.Key).Distinct().ToListAsync(cancellationToken);

        List<string> directPermissions = await (from userPerm in context.UserPermissions
                                                where userPerm.UserId == query.UserId && userPerm.TenantId == currentTenant.TenantId
                                                join permission in context.Permissions on userPerm.PermissionId equals permission.Id
                                                select permission.Key).Distinct().ToListAsync(cancellationToken);

        var all = rolePermissions.Concat(directPermissions).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return all;
    }
}
