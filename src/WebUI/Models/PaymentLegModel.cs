namespace WebUI.Models;

public class PaymentLegModel
{
    public Guid AccountId { get; set; }

    public string Currency { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public decimal ExchangeRate { get; set; }
}
