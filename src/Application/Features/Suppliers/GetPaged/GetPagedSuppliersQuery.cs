// <copyright file="GetPagedSuppliersQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.Common.Models;
using Application.Suppliers;

namespace Application.Suppliers.GetPaged;

public sealed record GetPagedSuppliersQuery(int Page, int PageSize, string? Search, bool? ActiveOnly)
    : IQuery<PaginatedList<SupplierResponse>>;
