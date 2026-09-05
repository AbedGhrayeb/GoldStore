// <copyright file="ToggleActiveEmployeeCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using SharedKernel.Result;

namespace Application.Employees.ToggleActive;

public sealed record ToggleActiveEmployeeCommand(Guid Id) : ICommand<Updated>;
