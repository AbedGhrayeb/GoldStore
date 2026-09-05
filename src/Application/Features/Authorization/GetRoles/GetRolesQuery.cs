// <copyright file="GetRolesQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Authorization.GetRoles;

public sealed record GetRolesQuery : IQuery<List<RoleResponse>>;
