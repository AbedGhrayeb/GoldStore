// <copyright file="UpdateExpenseCategoryCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using SharedKernel.Result;

namespace Application.Features.Expenses.ExpenseCategories.Update;

public sealed record UpdateExpenseCategoryCommand(Guid Id, string Name) : ICommand<Updated>;
