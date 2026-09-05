// <copyright file="RolePermission.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using SharedKernel;

namespace Domain.Authorization;

/// <summary>
/// Global join between a <see cref="Role"/> template and a <see cref="Permission"/>.
/// Belongs to neither tenant, exactly like the two aggregates it joins.
/// </summary>
public sealed class RolePermission : Entity
{
    public Guid RoleId { get; private set; }

    public Guid PermissionId { get; private set; }

    private RolePermission()
    {
    }

    public RolePermission(Guid id, Guid roleId, Guid permissionId)
        : base(id)
    {
        this.RoleId = roleId;
        this.PermissionId = permissionId;
    }
}
