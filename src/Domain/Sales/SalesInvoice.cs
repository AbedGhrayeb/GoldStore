using Domain.Common;
using Domain.Finance;
using SharedKernel;
using SharedKernel.Result;

namespace Domain.Sales;

public sealed class SalesInvoice : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    public string InvoiceNumber { get; private set; }

    public string CustomerName { get; private set; }

    public string? CustomerPhone { get; private set; }

    public DateTime Date { get; private set; }

    public Currency Currency { get; private set; }

    public decimal TotalAmount { get; private set; }

    public decimal AmountPaid { get; private set; }

    public decimal RemainingBalance { get; private set; }

    public PaymentMethod? PaymentMethod { get; private set; }


    public string? CustomerAccountNumber { get; private set; }

    public SalesInvoiceStatus Status { get; private set; }
    public string? Notes { get; set; }
    public Guid? EmployeeId { get; private set; }

    public Guid? AccountId { get; private set; }


    // Navigation Properties
    public FinancialAccount FinancialAccount { get; set; }
    private readonly List<SalesInvoiceItem> _saleInvoiceItems = [];
    public IEnumerable<SalesInvoiceItem> SaleInvoiceItems => _saleInvoiceItems;

    private SalesInvoice()
    {

    }
    private SalesInvoice(Guid id, string invoiceNumber, string customerName, string? customerPhone,
        DateTime date, Currency currency, decimal totalAmount, decimal amountPaid, PaymentMethod paymentMethod, SalesInvoiceStatus status,
        string? customerAccountNumber, string? note, Guid accountId, Guid employeeId, List<SalesInvoiceItem> items) : base(id)
    {
        InvoiceNumber = invoiceNumber;
        CustomerName = customerName;
        CustomerPhone = customerPhone;
        CustomerAccountNumber = customerAccountNumber;
        Date = date;
        Currency = currency;
        TotalAmount = totalAmount;
        AmountPaid = amountPaid;
        RemainingBalance = totalAmount - amountPaid;
        PaymentMethod = paymentMethod;
        Notes = note;
        AccountId = accountId;
        EmployeeId = employeeId;
        _saleInvoiceItems = items ?? new List<SalesInvoiceItem>();
    }
    public static Result<SalesInvoice> Create(Guid id, string invoiceNumber, string customerName, string? customerPhone,
        DateTime date, Currency currency, decimal totalAmount, decimal amountPaid, PaymentMethod paymentMethod, SalesInvoiceStatus status,
        string? customerAccountNumber, string? note, Guid accountId, Guid employeeId, List<SalesInvoiceItem> items)
    {

        if (string.IsNullOrEmpty(invoiceNumber))
        {
            return SalesInvoiceErrors.InvoceNumberRequired;
        }
        if (string.IsNullOrEmpty(customerName))
        {
            return SalesInvoiceErrors.InvoceNumberRequired;
        }
        if (employeeId == Guid.Empty)
        {
            return SalesInvoiceErrors.EmployeeIdRequired;
        }
        if (accountId == Guid.Empty)
        {
            return SalesInvoiceErrors.AccountIdRequired;
        }
        if (items == null || items.Count == 0)
        {
            return SalesInvoiceErrors.NoItems;
        }
        return new SalesInvoice(id, invoiceNumber, customerName, customerPhone, date,
            currency, totalAmount, amountPaid, paymentMethod, status,
            customerAccountNumber, note, accountId, employeeId, items);
    }
}
