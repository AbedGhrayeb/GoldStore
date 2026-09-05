// <copyright file="DebtKpiResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Features.Finance.Debts.GetKpis;

public sealed record DebtKpiResponse
{
    public decimal TotalReceivables { get; init; }

    public decimal TotalPayables { get; init; }

    public int ReceivableCount { get; init; }

    public int PayableCount { get; init; }

    public decimal NetBalance { get; init; }

    public string TotalReceivablesDisplay { get; init; } = "0.000";

    public string TotalPayablesDisplay { get; init; } = "0.000";

    public string NetBalanceDisplay { get; init; } = "0.000";

    public bool IsNetPositive { get; init; }

    public List<DebtTotalByCurrency> ByCurrency { get; init; } = [];
}

public sealed record DebtTotalByCurrency
{
    public string Currency { get; init; } = string.Empty;

    public string Symbol { get; init; } = string.Empty;

    public decimal TotalReceivables { get; init; }

    public decimal TotalPayables { get; init; }

    public decimal NetBalance { get; init; }

    public int ReceivableCount { get; init; }

    public int PayableCount { get; init; }

    public string TotalReceivablesDisplay { get; init; } = "0.000";

    public string TotalPayablesDisplay { get; init; } = "0.000";

    public string NetBalanceDisplay { get; init; } = "0.000";
}
