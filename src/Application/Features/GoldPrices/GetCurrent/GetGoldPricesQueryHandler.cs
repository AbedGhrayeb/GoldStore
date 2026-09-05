// <copyright file="GetGoldPricesQueryHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.Abstractions.Services;
using Domain.Common;
using SharedKernel;
using SharedKernel.Result;

namespace Application.Features.GoldPrices.GetCurrent;

internal sealed class GetGoldPricesQueryHandler(
    IGoldPriceService goldPriceService)
    : IQueryHandler<GetGoldPricesQuery, GoldPricesResponse>
{
    private static string FormatCurrency(decimal amount) => $"{amount:N0}";

    public async Task<Result<GoldPricesResponse>> Handle(GetGoldPricesQuery query, CancellationToken cancellationToken)
    {
        GoldPriceData? priceData = null;
        try
        {
            priceData = await goldPriceService.GetCurrentPricesAsync(Currency.JOD, cancellationToken);
        }
        catch
        {
            // If gold price API is unavailable, KPIs will show without live pricing
        }

        return new GoldPricesResponse
        {
            SpotPrice = new GoldPriceInfo
            {
                Price = priceData?.PricePerOunce ?? 0m,
                DisplayPrice = priceData is not null ? FormatCurrency(priceData.PricePerOunce) : "—",
                ChangePercent24H = priceData?.ChangePercent24H ?? 0m,
                ChangeDirection = (priceData?.ChangePercent24H ?? 0m) switch
                {
                    > 0 => "up",
                    < 0 => "down",
                    _ => "none",
                },
                Currency = "JOD",
                CurrencySymbol = "د.أ",
                Unit = "أونصة",
            },
            PricePerGram24K = new GoldPriceInfo
            {
                Price = priceData?.PricePerGram24K ?? 0m,
                DisplayPrice = priceData is not null ? $"{priceData.PricePerGram24K:F3}" : "—",
                ChangePercent24H = priceData?.ChangePercent24H ?? 0m,
                ChangeDirection = "none",
                Currency = "JOD",
                CurrencySymbol = "د.أ",
                Unit = "جم",
            },
            PricePerGram21K = new GoldPriceInfo
            {
                Price = priceData?.PricePerGram21K ?? 0m,
                DisplayPrice = priceData is not null ? $"{priceData.PricePerGram21K:F3}" : "—",
                ChangePercent24H = priceData?.ChangePercent24H ?? 0m,
                ChangeDirection = "none",
                Currency = "JOD",
                CurrencySymbol = "د.أ",
                Unit = "جم"
            },
        };
    }
}
