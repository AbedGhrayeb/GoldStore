// <copyright file="GetExpenseKpisQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.Expenses.Expenses.GetKpis;

public sealed record GetExpenseKpisQuery : IQuery<ExpenseKpiResponse>;
