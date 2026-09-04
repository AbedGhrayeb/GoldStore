using Domain.Common;
using Domain.Finance;
using SharedKernel;
using SharedKernel.Result;

namespace Domain.Employees;

public sealed class SalaryPayment : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    public const int DiscountRatePercentPerDay = 1;

    public Guid EmployeeId { get; private set; }

    public Guid AccountId { get; private set; }

    public Currency Currency { get; private set; }

    public decimal SalaryAmount { get; private set; }

    public decimal DiscountAmount { get; private set; }

    public decimal Amount { get; private set; }

    public DateOnly PaymentDate { get; private set; }

    public DateOnly ScheduledDate { get; private set; }

    public string? Notes { get; private set; }

    // Navigation properties
    public Employee Employee { get; set; }

    public FinancialAccount FinancialAccount { get; set; }

    private SalaryPayment()
    {
    }

    private SalaryPayment(Guid id, Guid employeeId, Guid accountId, Currency currency, decimal salaryAmount,
        decimal discountAmount, decimal amount, DateOnly paymentDate, DateOnly scheduledDate, string? notes) : base(id)
    {
        EmployeeId = employeeId;
        AccountId = accountId;
        Currency = currency;
        SalaryAmount = salaryAmount;
        DiscountAmount = discountAmount;
        Amount = amount;
        PaymentDate = paymentDate;
        ScheduledDate = scheduledDate;
        Notes = notes;
    }

    public static Result<SalaryPayment> Create(Guid employeeId, Guid accountId, Currency currency, decimal salaryAmount,
        decimal discountAmount, decimal amount, DateOnly paymentDate, DateOnly scheduledDate, string? notes)
    {
        if (employeeId == Guid.Empty)
        {
            return SalaryPaymentErrors.EmployeeIdRequired;
        }

        if (accountId == Guid.Empty)
        {
            return SalaryPaymentErrors.AccountIdRequired;
        }

        if (salaryAmount <= 0)
        {
            return SalaryPaymentErrors.SalaryNotSet;
        }

        if (discountAmount < 0)
        {
            return SalaryPaymentErrors.DiscountCannotBeNegative;
        }

        if (discountAmount >= salaryAmount)
        {
            return SalaryPaymentErrors.DiscountExceedsSalary;
        }

        if (amount <= 0)
        {
            return SalaryPaymentErrors.AmountMustBePositive;
        }

        if (amount > salaryAmount - discountAmount)
        {
            return SalaryPaymentErrors.AmountExceedsNet;
        }

        if (paymentDate == default)
        {
            return SalaryPaymentErrors.PaymentDateRequired;
        }

        if (scheduledDate == default)
        {
            return SalaryPaymentErrors.ScheduledDateRequired;
        }

        return new SalaryPayment(Guid.CreateVersion7(), employeeId, accountId, currency, salaryAmount,
            discountAmount, amount, paymentDate, scheduledDate, notes);
    }

    public static DateOnly GetScheduledDate(SalaryCycleEnum salaryCycle, DateOnly paymentDate)
    {
        return salaryCycle switch
        {
            SalaryCycleEnum.Weekly => paymentDate.AddDays(((int)DayOfWeek.Thursday - (int)paymentDate.DayOfWeek + 7) % 7),
            SalaryCycleEnum.Monthly => new DateOnly(paymentDate.Year, paymentDate.Month, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(salaryCycle), salaryCycle, null)
        };
    }

    public static DateOnly GetNextScheduledDate(SalaryCycleEnum salaryCycle, DateOnly fromDate)
    {
        return salaryCycle switch
        {
            SalaryCycleEnum.Weekly => fromDate.AddDays(((int)DayOfWeek.Thursday - (int)fromDate.DayOfWeek + 7) % 7),
            SalaryCycleEnum.Monthly => fromDate.Day <= 1
                ? new DateOnly(fromDate.Year, fromDate.Month, 1)
                : new DateOnly(fromDate.Year, fromDate.Month, 1).AddMonths(1),
            _ => throw new ArgumentOutOfRangeException(nameof(salaryCycle), salaryCycle, null)
        };
    }

    public static decimal CalculateDiscountAmount(decimal salaryAmount, int dayDifference)
    {
        if (dayDifference <= 0)
        {
            return 0m;
        }

        return decimal.Round(salaryAmount * DiscountRatePercentPerDay / 100m * dayDifference, 3);
    }
}
