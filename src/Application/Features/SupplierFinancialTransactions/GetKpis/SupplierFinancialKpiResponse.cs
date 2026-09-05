// <copyright file="SupplierFinancialKpiResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Features.SupplierFinancialTransactions.GetKpis;

public sealed record SupplierFinancialKpiResponse
{
    public List<SupplierFinancialKpiByCurrency> ByCurrency { get; init; } = [];
}

public sealed record SupplierFinancialKpiByCurrency
{
    public string Currency { get; init; } = string.Empty;

    public string Symbol { get; init; } = string.Empty;

    public decimal TotalFromSupplier { get; init; }

    public decimal TotalToSupplier { get; init; }

    public decimal NetBalance { get; init; }

    public int TransactionCount { get; init; }

    public string TotalFromSupplierDisplay { get; init; } = "0.000";

    public string TotalToSupplierDisplay { get; init; } = "0.000";

    public string NetBalanceDisplay { get; init; } = "0.000";
}
