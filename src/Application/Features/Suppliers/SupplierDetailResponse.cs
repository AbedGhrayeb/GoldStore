namespace Application.Suppliers;

public sealed record SupplierDetailResponse
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
    public List<FinancialBalanceByCurrency> FinancialBalancesByCurrency { get; init; } = [];
    public List<SupplierTransactionResponse> RecentTransactions { get; init; } = [];
}

public sealed record FinancialBalanceByCurrency
{
    public string Currency { get; init; } = string.Empty;
    public decimal Balance { get; init; }
}

public sealed record SupplierTransactionResponse
{
    public Guid Id { get; init; }
    public string Description { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public string Type { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Unit { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty;
}
