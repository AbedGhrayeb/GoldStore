// <copyright file="GetUserRolesQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Authorization.GetUserRoles;

public sealed record GetUserRolesQuery(Guid UserId) : IQuery<List<Guid>>;
