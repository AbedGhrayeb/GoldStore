// <copyright file="CreateEmployeeCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Employees;

namespace Application.Employees.Create;

public sealed record CreateEmployeeCommand(
    string FirstName,
    string LastName,
    RoleEnum Role,
    decimal Salary,
    Currency Currency,
    SalaryCycleEnum SalaryCycle,
    bool ConnectToUser,
    Guid? ExistingUserId,
    string? NewUserEmail,
    string? NewUserPassword) : ICommand<Guid>;
