namespace Application.Features.SalesInvoices.GetKpis;

public sealed record SalesInvoiceKpiResponse
{
    public int TodayCount { get; init; }
    public decimal TotalSales { get; init; }
    public string TotalSalesDisplay { get; init; } = "0.000";
    public decimal TotalPaid { get; init; }
    public string TotalPaidDisplay { get; init; } = "0.000";
    public decimal TotalRemaining { get; init; }
    public string TotalRemainingDisplay { get; init; } = "0.000";
}
