using Application.Features.StoreOperations.GetKpis;

namespace Application.Features.StoreOperations.GetTodayEmployeeStats;

public sealed record EmployeeDayStatsResponse
{
    public Guid EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public int SalesCount { get; init; }
    public decimal SalesWeight21K { get; init; }
    public List<CurrencyTotal> SalesTotals { get; init; } = [];
    public int PurchasesCount { get; init; }
    public decimal PurchasesWeight21K { get; init; }
    public List<CurrencyTotal> PurchasesTotals { get; init; } = [];
}
