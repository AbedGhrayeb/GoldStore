// <copyright file="UpdateExpenseCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using SharedKernel.Result;

namespace Application.Features.Expenses.Expenses.Update;

public sealed record UpdateExpenseCommand(
    Guid Id,
    DateOnly ExpenseDate,
    Guid? CategoryId,
    string? Description,
    decimal Amount,
    Guid AccountId) : ICommand<Updated>;
