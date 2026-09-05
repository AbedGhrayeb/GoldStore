// <copyright file="GetExpenseCategoriesQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.Expenses.ExpenseCategories.GetAll;

public sealed record GetExpenseCategoriesQuery(bool ActiveOnly = true) : IQuery<List<ExpenseCategoryResponse>>;
