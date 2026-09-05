// <copyright file="PagedSupplierFinancialTransactionResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Features.SupplierFinancialTransactions.GetPaged;

public sealed record PagedSupplierFinancialTransactionResponse
{
    public List<SupplierFinancialTransactionResponse> Items { get; init; } = [];

    public int TotalCount { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalPages => this.PageSize > 0 ? (int)Math.Ceiling((double)this.TotalCount / this.PageSize) : 0;
}

public sealed record SupplierFinancialTransactionResponse
{
    public Guid Id { get; init; }

    public Guid SupplierId { get; init; }

    public string SupplierName { get; init; } = string.Empty;

    public string Direction { get; init; } = string.Empty;

    public string DirectionLabel { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string Currency { get; init; } = string.Empty;

    public Guid AccountId { get; init; }

    public string? Notes { get; init; }

    public DateTime CreatedAt { get; init; }

    public decimal OutstandingBalance { get; init; }

    public string OutstandingBalanceDisplay { get; init; } = "0.000";
}
