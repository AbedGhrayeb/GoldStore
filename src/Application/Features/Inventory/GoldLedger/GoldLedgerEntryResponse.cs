namespace Application.Features.Inventory.GoldLedger;

public sealed record GoldLedgerEntryResponse
{
    public Guid Id { get; init; }
    public DateTime Date { get; init; }
    public int Karat { get; init; }
    public string KaratLabel { get; init; } = string.Empty;
    public decimal WeightInGrams { get; init; }
    public decimal Equivalent21KWeightInGrams { get; init; }
    public string MovementType { get; init; } = string.Empty;
    public string MovementLabel { get; init; } = string.Empty;
    public string MovementColor { get; init; } = string.Empty;
    public string MovementIcon { get; init; } = string.Empty;
    public string ReferenceType { get; init; } = string.Empty;
    public string ReferenceLabel { get; init; } = string.Empty;
    public Guid? ReferenceId { get; init; }
    public string? Notes { get; init; }
    public Guid UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
}