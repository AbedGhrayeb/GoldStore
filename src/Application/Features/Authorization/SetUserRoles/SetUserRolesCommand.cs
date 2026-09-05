// <copyright file="SetUserRolesCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Authorization.SetUserRoles;

public sealed record SetUserRolesCommand(Guid UserId, List<Guid> RoleIds) : ICommand<bool>;
