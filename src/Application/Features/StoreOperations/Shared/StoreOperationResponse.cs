namespace Application.Features.StoreOperations.Shared;

public sealed record StoreOperationResponse
{
    public Guid Id { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public string OperationType { get; init; } = string.Empty;
    public string OperationTypeLabel { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public string CounterpartyName { get; init; } = string.Empty;
    public string? CounterpartyPhone { get; init; }
    public string? EmployeeName { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string CurrencySymbol { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public decimal AmountPaid { get; init; }
    public decimal RemainingBalance { get; init; }
    public string? PaymentMethod { get; init; }
    public string? PaymentMethodLabel { get; init; }
    public Guid? AccountId { get; init; }
    public string? AccountName { get; init; }
    public string? Status { get; init; }
    public string? StatusLabel { get; init; }
    public int ItemsCount { get; init; }
    public string? Notes { get; init; }
}
