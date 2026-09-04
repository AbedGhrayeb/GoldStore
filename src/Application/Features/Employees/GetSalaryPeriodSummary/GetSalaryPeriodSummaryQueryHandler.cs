using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Domain.Employees;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Employees.GetSalaryPeriodSummary;

internal sealed class GetSalaryPeriodSummaryQueryHandler(IApplicationDbContext context, ICurrentTenant currentTenant)
    : IQueryHandler<GetSalaryPeriodSummaryQuery, SalaryPeriodSummaryResponse>
{
    public async Task<Result<SalaryPeriodSummaryResponse>> Handle(
        GetSalaryPeriodSummaryQuery query,
        CancellationToken cancellationToken)
    {
        Employee? employee = await context.Employees
            .AsNoTracking()
            .Where(e => e.TenantId == currentTenant.TenantId)
            .FirstOrDefaultAsync(e => e.Id == query.EmployeeId, cancellationToken);

        if (employee is null)
        {
            return SalaryPaymentErrors.EmployeeNotFound(query.EmployeeId);
        }

        if (employee.SalaryCycle == SalaryCycleEnum.Daily)
        {
            return SalaryPaymentErrors.DailyCycleNotSupported;
        }

        if (employee.Salary is not > 0m)
        {
            return SalaryPaymentErrors.SalaryNotSet;
        }

        DateOnly scheduledDate = SalaryPayment.GetScheduledDate(employee.SalaryCycle, query.PaymentDate);

        List<SalaryPayment> periodPayments = await context.SalaryPayments
            .AsNoTracking()
            .Where(p => p.TenantId == currentTenant.TenantId)
            .Where(p => p.EmployeeId == query.EmployeeId && p.ScheduledDate == scheduledDate)
            .ToListAsync(cancellationToken);

        decimal salaryAmount;
        decimal discountAmount;
        if (periodPayments.Count > 0)
        {
            salaryAmount = periodPayments[0].SalaryAmount;
            discountAmount = periodPayments[0].DiscountAmount;
        }
        else
        {
            int dayDifference = Math.Abs(query.PaymentDate.DayNumber - scheduledDate.DayNumber);
            salaryAmount = employee.Salary.Value;
            discountAmount = SalaryPayment.CalculateDiscountAmount(salaryAmount, dayDifference);
        }

        decimal paidSoFar = periodPayments.Sum(p => p.Amount);
        decimal periodNet = salaryAmount - discountAmount;

        return new SalaryPeriodSummaryResponse
        {
            EmployeeId = employee.Id,
            EmployeeName = employee.FullName,
            Salary = salaryAmount,
            SalaryCycle = employee.SalaryCycle,
            SalaryCycleName = employee.SalaryCycle.ToFriendlyString(),
            PaymentDate = query.PaymentDate,
            ScheduledDate = scheduledDate,
            DayOff = Math.Abs(query.PaymentDate.DayNumber - scheduledDate.DayNumber),
            DiscountAmount = discountAmount,
            NetAmount = periodNet,
            AlreadyPaid = paidSoFar,
            Remaining = periodNet - paidSoFar
        };
    }
}
