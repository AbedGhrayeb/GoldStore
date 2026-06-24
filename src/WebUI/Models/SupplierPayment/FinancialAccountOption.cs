namespace WebUI.Models.SupplierPayment;

public class FinancialAccountOption
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string DisplayLabel { get; set; } = string.Empty;
}