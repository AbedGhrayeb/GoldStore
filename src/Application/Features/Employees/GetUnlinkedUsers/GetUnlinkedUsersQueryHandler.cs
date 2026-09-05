// <copyright file="GetUnlinkedUsersQueryHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Employees.GetUnlinkedUsers;

internal sealed class GetUnlinkedUsersQueryHandler(IApplicationDbContext context, ICurrentTenant currentTenant)
    : IQueryHandler<GetUnlinkedUsersQuery, List<EmployeeUserOptionResponse>>
{
    public async Task<Result<List<EmployeeUserOptionResponse>>> Handle(
        GetUnlinkedUsersQuery query,
        CancellationToken cancellationToken)
    {
        List<Guid> linkedUserIds = await context.Employees
            .AsNoTracking()
            .Where(e => e.TenantId == currentTenant.TenantId)
            .Where(e => e.UserId.HasValue)
            .Select(e => e.UserId!.Value)
            .ToListAsync(cancellationToken);

        List<EmployeeUserOptionResponse> users = await context.Users
            .AsNoTracking()
            .Where(u => u.TenantId == currentTenant.TenantId)
            .Where(u => !linkedUserIds.Contains(u.Id))
            .OrderBy(u => u.FirstName)
            .Select(u => new EmployeeUserOptionResponse(
                u.Id,
                u.FirstName + " " + u.LastName,
                u.Email))
            .ToListAsync(cancellationToken);

        return users;
    }
}
