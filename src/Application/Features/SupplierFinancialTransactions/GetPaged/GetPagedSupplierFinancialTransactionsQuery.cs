// <copyright file="GetPagedSupplierFinancialTransactionsQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.SupplierFinancialTransactions.GetPaged;

public sealed record GetPagedSupplierFinancialTransactionsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? SupplierId = null,
    int? Direction = null,
    string? Search = null) : IQuery<PagedSupplierFinancialTransactionResponse>;
