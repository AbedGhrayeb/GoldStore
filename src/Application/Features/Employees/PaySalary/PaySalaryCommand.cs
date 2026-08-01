using Application.Abstractions.Messaging;

namespace Application.Employees.PaySalary;

public sealed record PaySalaryCommand(
    Guid EmployeeId,
    Guid AccountId,
    decimal Amount,
    DateOnly PaymentDate,
    string? Notes) : ICommand<Guid>;
