namespace Application.Finance.Accounts;

public sealed record AccountWithBalanceResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public string AccountType { get; init; } = string.Empty;
    public string? AccountNumber { get; init; }
    public bool IsActive { get; init; }
    public decimal Balance { get; init; }
    public decimal LastChangeAmount { get; init; }
    public string LastChangeDirection { get; init; } = string.Empty;
}