// <copyright file="GetUsersQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Users.GetAllUsers;

public sealed record GetUsersQuery : IQuery<List<UserResponse>>;
