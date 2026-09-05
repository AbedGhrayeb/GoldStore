// <copyright file="CreateExpenseCategoryCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.Expenses.ExpenseCategories.Create;

public sealed record CreateExpenseCategoryCommand(string Name) : ICommand<Guid>;
