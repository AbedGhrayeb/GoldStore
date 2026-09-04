namespace Application.Features.Finance.Debts;

public sealed record DebtResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string Direction { get; init; } = string.Empty;
    public string DirectionLabel { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public Guid? AccountId { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public decimal OutstandingBalance { get; init; }
    public string OutstandingBalanceDisplay { get; init; } = "0.000";
}
