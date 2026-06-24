using Domain.Common;
using Domain.Sales;
using SharedKernel;

namespace Domain.CustomerPurchases;

public sealed class CustomerPurchaseInvoice : Entity
{
    public Guid Id { get; set; }

    public required string InvoiceNumber { get; set; }

    public required string SellerName { get; set; }
    public required string SellerIdNumber { get; set; }

    public string? SellerPhone { get; set; }
    public int? SeelerYearOfBirth { get; set; }
    public string? SellerAddress { get; set; }

    public required string BuyerName { get; set; }

    public DateTime Date { get; set; }

    public Currency Currency { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal RemainingBalance => TotalAmount - AmountPaid;

    public PaymentMethod PaymentMethod { get; set; }

    public Guid AccountId { get; set; }

    public string? SellerAccountNumber { get; set; }

    public Guid UserId { get; set; }

    public string? Notes { get; set; }

}
