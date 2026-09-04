using Domain.Employees;

namespace Application.Employees.GetSalaryPeriodSummary;

public sealed record SalaryPeriodSummaryResponse
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public SalaryCycleEnum SalaryCycle { get; set; }
    public string SalaryCycleName { get; set; } = string.Empty;
    public DateOnly PaymentDate { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public int DayOff { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal AlreadyPaid { get; set; }
    public decimal Remaining { get; set; }
    public bool IsFullyPaid => Remaining <= 0;
}
