// <copyright file="GetUserPermissionsQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Authorization.GetUserPermissions;

public sealed record GetUserPermissionsQuery(Guid UserId) : IQuery<List<string>>;
