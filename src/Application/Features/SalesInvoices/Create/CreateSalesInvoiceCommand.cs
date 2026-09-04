using Application.Abstractions.Messaging;
using Application.Common.Ledger;

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
    Guid? EmployeeId,
    List<PaymentLegDto>? PaymentLegs,
    string? Notes) : ICommand<Guid>;

public sealed record SalesInvoiceItemDto(
    Guid? CategoryId,
    int Karat,
    decimal WeightInGrams,
    decimal PricePerGram);
