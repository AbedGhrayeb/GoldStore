// <copyright file="GetUserRolesQueryHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Authorization.GetUserRoles;

internal sealed class GetUserRolesQueryHandler(IApplicationDbContext context, ICurrentTenant currentTenant) : IQueryHandler<GetUserRolesQuery, List<Guid>>
{
    public async Task<Result<List<Guid>>> Handle(GetUserRolesQuery query, CancellationToken cancellationToken)
    {
        // Validate user exists and belongs to tenant
        bool userExists = await context.Users.AnyAsync(u => u.Id == query.UserId && u.TenantId == currentTenant.TenantId, cancellationToken);
        if (!userExists)
        {
            return UserErrors.NotFound(query.UserId);
        }

        List<Guid> roleIds = await context.UserRoles
            .Where(ur => ur.UserId == query.UserId && ur.TenantId == currentTenant.TenantId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        return roleIds;
    }
}
