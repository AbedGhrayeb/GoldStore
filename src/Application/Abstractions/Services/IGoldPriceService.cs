// <copyright file="IGoldPriceService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Common;

namespace Application.Abstractions.Services;

public interface IGoldPriceService
{
    Task<GoldPriceData> GetCurrentPricesAsync(Currency currency, CancellationToken cancellationToken);
}

public sealed record GoldPriceData(
    decimal PricePerOunce,
    decimal PricePerGram24K,
    decimal PricePerGram21K,
    decimal ChangePercent24H,
    DateTime Timestamp);
