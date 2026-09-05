// <copyright file="RoleResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Authorization.GetRoles;

public sealed record RoleResponse
{
    public Guid Id { get; init; }

    public string Key { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public List<string> PermissionKeys { get; init; } = [];
}
