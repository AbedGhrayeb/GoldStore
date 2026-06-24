using System.ComponentModel.DataAnnotations;

namespace WebUI.Models.CustomerPurchaseInvoice;

public class CreateCustomerPurchaseInvoiceModel
{
    [Required]
    [StringLength(200)]
    public string SellerName { get; set; } = string.Empty;

    [StringLength(20)]
    public string? SellerPhone { get; set; }
    [Required]
    [StringLength(20)]
    public string SellerIdNumber { get; set; }
    [Required]
    [MinLength(1940)]
    public int? SellerYearOfBirth{ get; set; }
    [StringLength(200)]
    public string? SellerAddress { get; set; }

    [Required]
    [StringLength(200)]
    public string BuyerName { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public string Currency { get; set; } = string.Empty;

    [Required]
    public List<CustomerPurchaseInvoiceItemModel> Items { get; set; } = [];

    [Required]
    [Range(0.001, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal AmountPaid { get; set; }

    [Required]
    public int PaymentMethod { get; set; }

    [Required]
    public Guid AccountId { get; set; }
    public string? SellerAccountNumber { get; set; }


    [StringLength(1000)]
    public string? Notes { get; set; }
}
