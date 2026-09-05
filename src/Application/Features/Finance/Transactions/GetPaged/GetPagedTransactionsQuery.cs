// <copyright file="GetPagedTransactionsQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.Common.Models;

namespace Application.Finance.Transactions.GetPaged;

public sealed record GetPagedTransactionsQuery(
    int Page = 1,
    int PageSize = 20,
    string? AccountName = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? Currency = null,
    string? AccountType = null) : IQuery<PaginatedList<RecentTransactionResponse>>;
