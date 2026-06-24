namespace WebUI.Models.Supplier;

public sealed class SupplierDetailModel
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
    public List<SupplierTransactionModel> RecentTransactions { get; set; } = [];
}

public sealed class SupplierTransactionModel
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
}
