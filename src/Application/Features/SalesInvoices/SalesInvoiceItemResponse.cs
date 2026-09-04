namespace Application.Features.SalesInvoices;

public sealed record SalesInvoiceItemResponse
{
    public Guid Id { get; init; }
    public string? CategoryName { get; init; }
    public int Karat { get; init; }
    public decimal WeightInGrams { get; init; }
    public decimal Equivalent21KWeightInGrams { get; init; }
    public decimal PricePerGram { get; init; }
    public decimal GoldAmount { get; init; }
}
