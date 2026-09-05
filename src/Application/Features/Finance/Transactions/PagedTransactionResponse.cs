// <copyright file="PagedTransactionResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Finance.Transactions;

public sealed record PagedTransactionResponse
{
    public List<RecentTransactionResponse> Items { get; init; } = [];

    public int TotalCount { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalPages => (int)Math.Ceiling((double)this.TotalCount / this.PageSize);
}
