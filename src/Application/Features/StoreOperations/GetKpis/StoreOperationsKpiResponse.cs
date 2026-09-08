// <copyright file="StoreOperationsKpiResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Features.StoreOperations.GetKpis;

public sealed record StoreOperationsKpiResponse
{
    public int TodaySalesCount { get; init; }

    public int TodayPurchasesCount { get; init; }

    public List<CurrencyTotal> TodaySalesTotals { get; init; } = [];

    public List<CurrencyTotal> TodayPurchasesTotals { get; init; } = [];

    public List<CategoryKpi> SalesByCategory { get; init; } = [];

    public List<CategoryKpi> PurchasesByCategory { get; init; } = [];
}

public sealed record CurrencyTotal(string Currency, string Symbol, decimal Amount);

public sealed record CategoryKpi(
    Guid? CategoryId,
    string CategoryName,
    decimal WeightInGrams,
    int Count,
    List<CurrencyTotal> Totals);
