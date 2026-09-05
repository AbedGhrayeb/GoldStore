// <copyright file="GetGoldLedgerQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.Common.Models;

namespace Application.Features.Inventory.GoldLedger.GetPaged;

public sealed record GetGoldLedgerQuery(
    int Page = 1,
    int PageSize = 20,
    int? Karat = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? ReferenceType = null) : IQuery<PaginatedList<GoldLedgerEntryResponse>>;
