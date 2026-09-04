namespace Application.Features.Expenses.Expenses;

public sealed record ExpenseResponse
{
    public Guid Id { get; init; }
    public DateOnly ExpenseDate { get; init; }
    public Guid? CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string CurrencySymbol { get; init; } = string.Empty;
    public Guid AccountId { get; init; }
    public string AccountName { get; init; } = string.Empty;
}
