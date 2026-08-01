using Application.Abstractions.Messaging;

namespace Application.Features.CustomerPurchaseInvoices.Create;

public sealed record CreateCustomerPurchaseInvoiceCommand(
    string SellerName,
    string? SellerPhone,
    string SellerIdNumber,
    int? SellerYearOfBirth,
    string? SellerAddress,
    Guid EmployeeId,
    string Currency,
    DateTime Date,
    decimal TotalAmount,
    decimal AmountPaid,
    int PaymentMethod,
    Guid AccountId,
    string? SellerAccountNumber,
    string? Notes,
    List<CustomerPurchaseInvoiceItemDto> Items
) : ICommand<Guid>;

public sealed record CustomerPurchaseInvoiceItemDto(
    Guid? CategoryId,
    int Karat,
    decimal WeightInGrams,
    decimal PricePerGram);
