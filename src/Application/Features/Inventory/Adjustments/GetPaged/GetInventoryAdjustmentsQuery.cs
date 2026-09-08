// <copyright file="GetInventoryAdjustmentsQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.Common.Models;

namespace Application.Features.Inventory.Adjustments.GetPaged;

public sealed record GetInventoryAdjustmentsQuery(
    int Page = 1,
    int PageSize = 20,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? AdjustmentType = null,
    string? Search = null,
    int? Karat = null) : IQuery<PaginatedList<InventoryAdjustmentResponse>>;
