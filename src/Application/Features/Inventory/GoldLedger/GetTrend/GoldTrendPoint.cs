// <copyright file="GoldTrendPoint.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Features.Inventory.GoldLedger.GetTrend;

public sealed record GoldTrendPoint
{
    public DateTime Date { get; init; }

    public string Label { get; init; } = string.Empty;

    public decimal In21K { get; init; }

    public decimal Out21K { get; init; }

    public decimal Net21K { get; init; }
}
