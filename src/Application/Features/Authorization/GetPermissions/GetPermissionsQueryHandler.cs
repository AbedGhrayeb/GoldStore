// <copyright file="GetPermissionsQueryHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Authorization.GetPermissions;

internal sealed class GetPermissionsQueryHandler(IApplicationDbContext context) : IQueryHandler<GetPermissionsQuery, List<PermissionResponse>>
{
    public async Task<Result<List<PermissionResponse>>> Handle(GetPermissionsQuery query, CancellationToken cancellationToken)
    {
        List<PermissionResponse> perms = await context.Permissions.AsNoTracking()
            .Select(p => new PermissionResponse { Id = p.Id, Key = p.Key, Name = p.Name })
            .ToListAsync(cancellationToken);
        return perms;
    }
}
