namespace Application.Features.Inventory.GoldLedger.GetKpis;

public sealed record InventoryKpiResponse
{
    public GoldPriceInfo SpotPrice { get; init; } = new();
    public GoldPriceInfo PricePerGram24K { get; init; } = new();
    public GoldPriceInfo PricePerGram21K { get; init; } = new();
    public decimal TotalEquivalent21KGrams { get; init; }
    public string TotalEquivalent21KDisplay { get; init; } = "0.000";
    public decimal EstimatedValueJod { get; init; }
    public string EstimatedValueDisplay { get; init; } = "0.000";
    public List<KaratBreakdown> KaratBreakdowns { get; init; } = [];
}

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

public sealed record KaratBreakdown
{
    public int Karat { get; init; }
    public string KaratLabel { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal TotalWeightGrams { get; init; }
    public string TotalWeightDisplay { get; init; } = "0.000";
    public bool IsPrimary { get; init; }
}