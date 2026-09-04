using Domain.Common;
using Domain.Finance;
using Domain.Sales;
using SharedKernel;
using SharedKernel.Result;

namespace Domain.CustomerPurchases;

public sealed class CustomerPurchaseInvoice : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    public string InvoiceNumber { get; private set; }

    public string SellerName { get; private set; }
    public string? SellerIdNumber { get; private set; }

    public string? SellerPhone { get; private set; }
    public int? SellerYearOfBirth { get; private set; }
    public string? SellerAddress { get; private set; }

    public Guid? EmployeeId { get; private set; }

    public DateTime? Date { get; private set; }

    public Currency Currency { get; private set; }

    public decimal TotalAmount { get; private set; }

    public decimal AmountPaid { get; private set; }

    public decimal RemainingBalance => TotalAmount - AmountPaid;

    public PaymentMethod PaymentMethod { get; private set; }

    public Guid AccountId { get; private set; }

    public string? SellerAccountNumber { get; private set; }

    public string? Notes { get; private set; }
    //relationships
    public FinancialAccount FinancialAccount { get; set; }
    private readonly List<CustomerPurchaseInvoiceItem> _items = new();
    public IReadOnlyCollection<CustomerPurchaseInvoiceItem> Items => _items.AsReadOnly();
    public CustomerPurchaseInvoice()
    {

    }
    public CustomerPurchaseInvoice(Guid id, string invoiceNumber, string sellerName, string? sellerIdNumber,
    string? sellerPhone, int? sellerYearOfBirth, string? sellerAddress, Guid employeeId, DateTime? date,
    Currency currency, decimal totalAmount, decimal amountPaid, PaymentMethod paymentMethod, Guid accountId,
    string? sellerAccountNumber, string? notes, List<CustomerPurchaseInvoiceItem> items) : base(id)
    {
        InvoiceNumber = invoiceNumber;
        SellerName = sellerName;
        SellerIdNumber = sellerIdNumber;
        SellerPhone = sellerPhone;
        SellerYearOfBirth = sellerYearOfBirth;
        SellerAddress = sellerAddress;
        EmployeeId = employeeId;
        Date = date;
        Currency = currency;
        TotalAmount = totalAmount;
        AmountPaid = amountPaid;
        PaymentMethod = paymentMethod;
        AccountId = accountId;
        SellerAccountNumber = sellerAccountNumber;
        Notes = notes;
        _items = items ?? new List<CustomerPurchaseInvoiceItem>();
    }
    public static Result<CustomerPurchaseInvoice> Create(Guid id, string invoiceNumber, string sellerName, string? sellerIdNumber, string? sellerPhone,
        int? sellerYearOfBirth, string? sellerAddress, Guid employeeId, DateTime? date, Currency currency, decimal totalAmount,
        decimal amountPaid, PaymentMethod paymentMethod, Guid accountId, string? sellerAccountNumber, string? notes, List<CustomerPurchaseInvoiceItem> items)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            return CustomerPurchaseInvoiceErrors.InvoiceNumberRequired;
        }
        if (string.IsNullOrWhiteSpace(sellerName))
        {
            return CustomerPurchaseInvoiceErrors.SellerNameRequired;
        }
        if (employeeId == Guid.Empty)
        {
            return CustomerPurchaseInvoiceErrors.EmployeeIdRequired;
        }
        if (accountId == Guid.Empty)
        {
            return CustomerPurchaseInvoiceErrors.AccountIdRequired;
        }

        if (items == null || items.Count == 0)
        {
            return CustomerPurchaseInvoiceErrors.NoItems;
        }
        return new CustomerPurchaseInvoice(id, invoiceNumber, sellerName, sellerIdNumber, sellerPhone, sellerYearOfBirth, sellerAddress, employeeId, date, currency, totalAmount, amountPaid, paymentMethod, accountId, sellerAccountNumber, notes, items);
    }

}
