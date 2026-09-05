// <copyright file="GetPermissionsQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Authorization.GetPermissions;

public sealed record GetPermissionsQuery : IQuery<List<PermissionResponse>>;
