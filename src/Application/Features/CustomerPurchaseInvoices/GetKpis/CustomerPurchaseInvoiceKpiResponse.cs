namespace Application.Features.CustomerPurchaseInvoices.GetKpis;

public sealed record CustomerPurchaseInvoiceKpiResponse
{
    public int TodayCount { get; init; }
    public decimal TotalPurchases { get; init; }
    public string TotalPurchasesDisplay { get; init; } = "0.000";
    public decimal TotalPaid { get; init; }
    public string TotalPaidDisplay { get; init; } = "0.000";
    public decimal TotalRemaining { get; init; }
    public string TotalRemainingDisplay { get; init; } = "0.000";
}
