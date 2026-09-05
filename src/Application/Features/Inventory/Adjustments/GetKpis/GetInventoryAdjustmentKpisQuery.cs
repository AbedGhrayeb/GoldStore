// <copyright file="GetInventoryAdjustmentKpisQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.Inventory.Adjustments.GetKpis;

public sealed record GetInventoryAdjustmentKpisQuery : IQuery<InventoryAdjustmentKpiResponse>;
