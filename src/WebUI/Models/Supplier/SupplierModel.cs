namespace WebUI.Models.Supplier;

public sealed class SupplierModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PrimaryPhone { get; set; } = string.Empty;
    public string? SecondaryPhone { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal GoldBalance { get; set; }
    public decimal ManufacturingBalance { get; set; }
    public DateTime? LastTransactionDate { get; set; }
}
