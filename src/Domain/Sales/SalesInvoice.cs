using Domain.Common;
using SharedKernel;

namespace Domain.Sales;

public sealed class SalesInvoice : Entity
{
    public Guid Id { get; set; }

    public required string InvoiceNumber { get; set; }

    public required string CustomerName { get; set; }

    public string? CustomerPhone { get; set; }

    public DateTime Date { get; set; }

    public Currency Currency { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal RemainingBalance { get; set; }

    public PaymentMethod? PaymentMethod { get; set; }

    public Guid? AccountId { get; set; }

    public string? BuyerAccountNumber { get; set; }

    public string? SellerName { get; set; }

    public SalesInvoiceStatus Status { get; set; }

    public Guid UserId { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}
