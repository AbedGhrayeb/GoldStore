// <copyright file="Permission.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using SharedKernel;
using SharedKernel.Result;

namespace Domain.Authorization;

/// <summary>
/// A global permission definition (plan Phase 0/4). Permissions are seeded reference
/// data shared by every tenant; they are never tenant-owned. Roles (global templates)
/// grant permissions, and each tenant assigns roles to its own users via <c>UserRole</c>.
/// </summary>
public sealed class Permission : Entity
{
    public string Key { get; private set; }

    public string Name { get; private set; }

    private Permission()
    {
        this.Key = string.Empty;
        this.Name = string.Empty;
    }

    private Permission(Guid id, string key, string name)
        : base(id)
    {
        this.Key = key;
        this.Name = name;
    }

    public static Result<Permission> Create(string key, string name)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return AuthorizationErrors.KeyRequired;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return AuthorizationErrors.NameRequired;
        }

        return new Permission(Guid.CreateVersion7(), key.Trim().ToLowerInvariant(), name.Trim());
    }
}
