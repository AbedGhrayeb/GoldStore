// <copyright file="DeleteExpenseCategoryCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using SharedKernel.Result;

namespace Application.Features.Expenses.ExpenseCategories.Delete;

public sealed record DeleteExpenseCategoryCommand(Guid Id) : ICommand<Deleted>;
