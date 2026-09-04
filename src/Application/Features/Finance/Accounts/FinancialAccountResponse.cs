namespace Application.Finance.Accounts;

public sealed record FinancialAccountResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public string AccountType { get; init; } = string.Empty;
    public string? AccountNumber { get; init; }
    public bool IsActive { get; init; }
}
