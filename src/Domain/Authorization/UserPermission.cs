// <copyright file="UserPermission.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Users;
using SharedKernel;
using SharedKernel.Result;

namespace Domain.Authorization;

/// <summary>
/// Tenant-scoped direct permission grant to a user (granular override).
/// Complements role-based grants; effective permissions are union of role + direct.
/// </summary>
public sealed class UserPermission : Entity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid PermissionId { get; private set; }

    private UserPermission()
    {
    }

    private UserPermission(Guid id, Guid tenantId, Guid userId, Guid permissionId)
        : base(id)
    {
        this.TenantId = tenantId;
        this.UserId = userId;
        this.PermissionId = permissionId;
    }

    public static Result<UserPermission> Create(Guid tenantId, Guid userId, Guid permissionId)
    {
        if (tenantId == Guid.Empty)
        {
            return UserErrors.TenantRequired;
        }

        if (userId == Guid.Empty)
        {
            return UserErrors.IdRequired;
        }

        if (permissionId == Guid.Empty)
        {
            return AuthorizationErrors.KeyRequired;
        }

        return new UserPermission(Guid.CreateVersion7(), tenantId, userId, permissionId);
    }
}
