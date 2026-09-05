// <copyright file="GetStoreEmployeesQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.StoreOperations.GetEmployees;

public sealed record GetStoreEmployeesQuery : IQuery<List<EmployeeResponse>>;
