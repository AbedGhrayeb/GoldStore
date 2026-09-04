namespace Infrastructure.GoldPrices;

public sealed class GoldApiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://www.goldapi.io/api";
    public int CacheDurationMinutes { get; set; } = 15;
}
