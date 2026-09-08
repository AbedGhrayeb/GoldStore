// <copyright file="GetSupplierTransactionsQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.Common.Models;
using Application.Suppliers;

namespace Application.Suppliers.GetTransactions;

public sealed record GetSupplierTransactionsQuery(Guid SupplierId, string? Type, int Page, int PageSize)
    : IQuery<PaginatedList<SupplierTransactionResponse>>;
