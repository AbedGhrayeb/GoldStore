using Application.Features.GoldPrices;

namespace Application.Features.Inventory.GoldLedger.GetKpis;

public sealed record InventoryKpiResponse
{
    public decimal TotalEquivalent21KGrams { get; init; }
    public string TotalEquivalent21KDisplay { get; init; } = "0.000";
    public string TotalEquivalent21KUnit { get; init; } = "جم";
    public decimal EstimatedValueJod { get; init; }
    public string EstimatedValueDisplay { get; init; } = "0.000";
    public List<KaratBreakdown> KaratBreakdowns { get; init; } = [];
}

public sealed record KaratBreakdown
{
    public int Karat { get; init; }
    public string KaratLabel { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal TotalWeightGrams { get; init; }
    public string TotalWeightDisplay { get; init; } = "0.000";
    public string Unit { get; init; } = "جم";
    public bool IsPrimary { get; init; }
}
