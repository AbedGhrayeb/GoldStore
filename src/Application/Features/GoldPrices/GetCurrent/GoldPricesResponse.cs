// <copyright file="GoldPricesResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Features.GoldPrices.GetCurrent;

public sealed record GoldPricesResponse
{
    public GoldPriceInfo SpotPrice { get; init; } = new();

    public GoldPriceInfo PricePerGram24K { get; init; } = new();

    public GoldPriceInfo PricePerGram21K { get; init; } = new();
}
