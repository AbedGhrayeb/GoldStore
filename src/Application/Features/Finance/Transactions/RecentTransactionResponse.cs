namespace Application.Finance.Transactions;

public sealed record RecentTransactionResponse
{
    public Guid Id { get; init; }
    public DateTime Date { get; init; }
    public string Description { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string TransactionType { get; init; } = string.Empty;
    public string ReferenceType { get; init; } = string.Empty;
}
