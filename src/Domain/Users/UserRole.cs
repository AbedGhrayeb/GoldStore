// <copyright file="UserRole.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using SharedKernel;
using SharedKernel.Result;

namespace Domain.Users;

/// <summary>
/// Tenant-scoped assignment of a global role template to a tenant user (plan
/// Phase 0 item 9). The <see cref="TenantId"/> scopes the assignment: the same
/// user and role pair can only exist once per tenant.
/// </summary>
public sealed class UserRole : Entity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    private UserRole()
    {
    }

    private UserRole(Guid id, Guid tenantId, Guid userId, Guid roleId)
        : base(id)
    {
        this.TenantId = tenantId;
        this.UserId = userId;
        this.RoleId = roleId;
    }

    public static Result<UserRole> Create(Guid tenantId, Guid userId, Guid roleId)
    {
        if (tenantId == Guid.Empty)
        {
            return UserErrors.TenantRequired;
        }

        if (userId == Guid.Empty)
        {
            return UserErrors.IdRequired;
        }

        if (roleId == Guid.Empty)
        {
            return UserErrors.RoleRequired;
        }

        return new UserRole(Guid.CreateVersion7(), tenantId, userId, roleId);
    }
}
