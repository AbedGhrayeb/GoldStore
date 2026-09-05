// <copyright file="GetGoldLedgerTrendQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.Inventory.GoldLedger.GetTrend;

public sealed record GetGoldLedgerTrendQuery(int Days = 7) : IQuery<List<GoldTrendPoint>>;
