namespace Application.Features.SupplierFinancialTransactions.GetPayments;

public sealed record SupplierFinancialPaymentResponse
{
    public Guid Id { get; init; }
    public decimal Amount { get; init; }
    public string AccountName { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public string? Notes { get; init; }
}
