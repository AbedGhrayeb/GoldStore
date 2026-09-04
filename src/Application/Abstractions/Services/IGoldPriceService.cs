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
    decimal PricePerGram18K,
    decimal ChangePercent24H,
    DateTime Timestamp);
