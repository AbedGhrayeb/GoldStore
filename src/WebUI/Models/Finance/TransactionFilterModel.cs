namespace WebUI.Models.Finance;

public class TransactionFilterModel
{
    public string? AccountName { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Currency { get; set; }
    public string? AccountType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}