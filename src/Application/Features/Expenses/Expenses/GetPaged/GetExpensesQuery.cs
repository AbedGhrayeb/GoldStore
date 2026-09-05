// <copyright file="GetExpensesQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.Common.Models;

namespace Application.Features.Expenses.Expenses.GetPaged;

public sealed record GetExpensesQuery(
    int Page = 1,
    int PageSize = 20,
    string? AccountName = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    Guid? CategoryId = null) : IQuery<PaginatedList<ExpenseResponse>>;
