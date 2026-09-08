// <copyright file="GoldPriceService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Text.Json;
using Application.Abstractions.Services;
using Domain.Common;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.GoldPrices;

internal sealed class GoldPriceService(
    IHttpClientFactory httpClientFactory,
    IOptions<GoldApiOptions> options,
    HybridCache cache,
    ILogger<GoldPriceService> logger) : IGoldPriceService
{
    private readonly GoldApiOptions options = options.Value;

    public async Task<GoldPriceData> GetCurrentPricesAsync(Currency currency, CancellationToken cancellationToken)
    {
        // The global price feed is JOD-only and refreshed once per day: a fixed absolute
        // expiration guarantees at most one GoldAPI call per day. The key deliberately
        // carries no tenant id (shared external feed, not tenant-owned data).
        // LocalCacheExpiration beyond Expiration enables stale-while-revalidate, so a
        // GoldAPI outage degrades to the last known price while a refresh is attempted.
        const string cacheKey = "GoldPrice_JOD";

        HybridCacheEntryOptions cacheOptions = new()
        {
            Expiration = TimeSpan.FromHours(24),
            LocalCacheExpiration = TimeSpan.FromHours(48),
        };

        return await cache.GetOrCreateAsync<GoldPriceData>(
            cacheKey,
            (token) => FetchFromApiAsync(token),
            cacheOptions,
            cancellationToken: cancellationToken);
    }

    private async ValueTask<GoldPriceData> FetchFromApiAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException(
                "GoldAPI key is not configured. Set GoldApi__ApiKey via user-secrets (dev) or environment variables (production).");
        }

        HttpClient httpClient = httpClientFactory.CreateClient("GoldApi");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/XAU/JOD");
        request.Headers.Add("x-access-token", options.ApiKey);

        HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("GoldAPI request failed with status {StatusCode}", response.StatusCode);
            throw new InvalidOperationException($"GoldAPI request failed with status {response.StatusCode}");
        }

        string jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);

        using var doc = JsonDocument.Parse(jsonResponse);
        JsonElement root = doc.RootElement;

        if (!root.TryGetProperty("price", out JsonElement priceElement))
        {
            string providerError = root.TryGetProperty("error", out JsonElement errorElement)
                ? errorElement.GetString() ?? "unknown provider error"
                : "unexpected provider payload";
            logger.LogWarning("GoldAPI returned no price: {ProviderError}", providerError);
            throw new InvalidOperationException($"GoldAPI returned no price: {providerError}");
        }

        decimal pricePerOunce = priceElement.GetDecimal();
        decimal priceGram24K = root.TryGetProperty("price_gram_24k", out JsonElement p24) ? p24.GetDecimal() : 0m;
        decimal priceGram21K = root.TryGetProperty("price_gram_21k", out JsonElement p21) ? p21.GetDecimal() : 0m;
        decimal changePercent = root.TryGetProperty("chg_percent", out JsonElement chg) ? chg.GetDecimal() : 0m;

        return new GoldPriceData(
            PricePerOunce: pricePerOunce,
            PricePerGram24K: priceGram24K,
            PricePerGram21K: priceGram21K,
            ChangePercent24H: changePercent,
            Timestamp: DateTime.UtcNow);
    }
}
