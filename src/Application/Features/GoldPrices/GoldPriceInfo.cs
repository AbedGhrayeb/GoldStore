namespace Application.Features.GoldPrices;

public sealed record GoldPriceInfo
{
    public decimal Price { get; init; }
    public string DisplayPrice { get; init; } = "0.000";
    public decimal ChangePercent24H { get; init; }
    public string ChangeDirection { get; init; } = "none";
    public string Currency { get; init; } = "JOD";
    public string CurrencySymbol { get; init; } = "د.أ";
    public string Unit { get; init; } = string.Empty;
}