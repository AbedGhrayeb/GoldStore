namespace Application.Features.CustomerPurchaseInvoices;

public sealed record CustomerPurchaseInvoiceResponse
{
    public Guid Id { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public string SellerName { get; init; } = string.Empty;
    public string? SellerPhone { get; init; }
    public string? SellerIdNumber { get; init; }
    public int? SellerYearOfBirth { get; init; }
    public string? SellerAddress { get; init; }
    public string EmployeeName { get; init; } = "—";
    public DateTime? Date { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public decimal AmountPaid { get; init; }
    public decimal RemainingBalance { get; init; }
    public string? PaymentMethod { get; init; }
    public string? SellerAccountNumber { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<CustomerPurchaseInvoiceItemResponse> Items { get; init; } = [];
}

public sealed record CustomerPurchaseInvoiceItemResponse
{
    public Guid Id { get; init; }
    public Guid? CategoryId { get; init; }
    public int Karat { get; init; }
    public decimal WeightInGrams { get; init; }
    public decimal Equivalent21KWeightInGrams { get; init; }
    public decimal PricePerGram { get; init; }
    public decimal GoldAmount { get; init; }
}
