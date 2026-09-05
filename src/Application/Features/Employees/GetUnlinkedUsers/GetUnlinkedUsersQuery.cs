// <copyright file="GetUnlinkedUsersQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Employees.GetUnlinkedUsers;

public sealed record GetUnlinkedUsersQuery : IQuery<List<EmployeeUserOptionResponse>>;
