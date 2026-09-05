// <copyright file="GetInventoryKpisQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.Inventory.GoldLedger.GetKpis;

public sealed record GetInventoryKpisQuery : IQuery<InventoryKpiResponse>;
