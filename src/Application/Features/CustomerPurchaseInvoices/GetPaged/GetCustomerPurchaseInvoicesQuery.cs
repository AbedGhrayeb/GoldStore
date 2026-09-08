// <copyright file="GetCustomerPurchaseInvoicesQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.Common.Models;

namespace Application.Features.CustomerPurchaseInvoices.GetPaged;

public sealed record GetCustomerPurchaseInvoicesQuery(
    int Page = 1,
    int PageSize = 20,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? Search = null,
    Guid? CategoryId = null) : IQuery<PaginatedList<CustomerPurchaseInvoiceResponse>>;
