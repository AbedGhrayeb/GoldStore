using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Authorization.GetRoles;

internal sealed class GetRolesQueryHandler(IApplicationDbContext context) : IQueryHandler<GetRolesQuery, List<RoleResponse>>
{
    public async Task<Result<List<RoleResponse>>> Handle(GetRolesQuery query, CancellationToken cancellationToken)
    {
        List<Role> roles = await context.Roles.AsNoTracking().ToListAsync(cancellationToken);
        List<RolePermission> rolePermissions = await context.RolePermissions.AsNoTracking().ToListAsync(cancellationToken);
        Dictionary<Guid, string> permissions = await context.Permissions.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Key, cancellationToken);

        var result = roles.Select(r => new RoleResponse
        {
            Id = r.Id,
            Key = r.Key,
            Name = r.Name,
            PermissionKeys = rolePermissions
                .Where(rp => rp.RoleId == r.Id)
                .Select(rp => permissions.TryGetValue(rp.PermissionId, out string? key) ? key : string.Empty)
                .Where(k => !string.IsNullOrEmpty(k))
                .ToList()
        }).ToList();

        return result;
    }
}
