using Application.Abstractions.Messaging;

namespace Application.Features.SalesInvoices.Create;

public sealed record CreateSalesInvoiceCommand(
    string CustomerName,
    string? CustomerPhone,
    DateTime Date,
    string Currency,
    List<SalesInvoiceItemDto> Items,
    decimal TotalAmount,
    decimal AmountPaid,
    int? PaymentMethod,
    Guid? AccountId,
    string? BuyerAccountNumber,
    string? SellerName,
    string? Notes) : ICommand<Guid>;

public sealed record SalesInvoiceItemDto(
    Guid? CategoryId,
    int Karat,
    decimal WeightInGrams,
    decimal PricePerGram);
