using Application.Abstractions.Messaging;

namespace Application.Employees.GetSalaryPeriodSummary;

public sealed record GetSalaryPeriodSummaryQuery(
    Guid EmployeeId,
    DateOnly PaymentDate) : IQuery<SalaryPeriodSummaryResponse>;
