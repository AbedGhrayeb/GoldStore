namespace Application.Suppliers;

public sealed record SupplierResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string PrimaryPhone { get; init; } = string.Empty;
    public string? SecondaryPhone { get; init; }
    public string? BankAccountNumber { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public decimal GoldBalance { get; init; }
    public decimal ManufacturingBalance { get; init; }
    public DateTime? LastTransactionDate { get; init; }
}
