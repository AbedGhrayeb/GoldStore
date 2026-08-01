using System.Text.Json;
using Application.Abstractions.Services;
using Domain.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Infrastructure.GoldPrices;

internal sealed class GoldPriceService(
    IHttpClientFactory httpClientFactory,
    IOptions<GoldApiOptions> options,
    IMemoryCache cache) : IGoldPriceService
{
    private readonly GoldApiOptions _options = options.Value;

    public async Task<GoldPriceData> GetCurrentPricesAsync(Currency currency, CancellationToken cancellationToken)
    {
        string cacheKey = $"GoldPrice_{currency}";

        if (cache.TryGetValue(cacheKey, out GoldPriceData? cached) && cached is not null)
        {
            return cached;
        }

        string currencyCode = currency switch
        {
            Currency.JOD => "JOD",
            Currency.USD => "USD",
            Currency.ILS => "ILS",
            _ => "USD"
        };

        HttpClient httpClient = httpClientFactory.CreateClient("GoldApi");
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/XAU/{currencyCode}");
        request.Headers.Add("x-access-token", _options.ApiKey);

        HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // Try fallback to last cached value even if expired
            if (cache.TryGetValue(cacheKey, out GoldPriceData? fallback) && fallback is not null)
            {
                return fallback;
            }

            throw new InvalidOperationException($"GoldAPI request failed with status {response.StatusCode}");
        }

        string jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);

        using var doc = JsonDocument.Parse(jsonResponse);
        JsonElement root = doc.RootElement;

        decimal pricePerOunce = root.GetProperty("price").GetDecimal();
        decimal priceGram24K = root.TryGetProperty("price_gram_24k", out JsonElement p24) ? p24.GetDecimal() : 0m;
        decimal priceGram21K = root.TryGetProperty("price_gram_21k", out JsonElement p21) ? p21.GetDecimal() : 0m;
        decimal priceGram18K = root.TryGetProperty("price_gram_18k", out JsonElement p18) ? p18.GetDecimal() : 0m;
        decimal changePercent = root.TryGetProperty("chg_percent", out JsonElement chg) ? chg.GetDecimal() : 0m;

        var data = new GoldPriceData(
            PricePerOunce: pricePerOunce,
            PricePerGram24K: priceGram24K,
            PricePerGram21K: priceGram21K,
            PricePerGram18K: priceGram18K,
            ChangePercent24H: changePercent,
            Timestamp: DateTime.UtcNow);

        MemoryCacheEntryOptions cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(_options.CacheDurationMinutes));

        cache.Set(cacheKey, data, cacheOptions);

        return data;
    }
}
