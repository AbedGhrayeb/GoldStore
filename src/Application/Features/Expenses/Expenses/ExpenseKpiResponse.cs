// <copyright file="ExpenseKpiResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Features.Expenses.Expenses;

public sealed record ExpenseKpiResponse
{
    public List<CurrencyTotal> TodayTotals { get; init; } = [];

    public List<CurrencyTotal> MonthTotals { get; init; } = [];

    public string TopCategoryName { get; init; } = string.Empty;

    public List<CurrencyTotal> TopCategoryAmounts { get; init; } = [];
}

public sealed record CurrencyTotal(string Currency, string Symbol, decimal Amount);
