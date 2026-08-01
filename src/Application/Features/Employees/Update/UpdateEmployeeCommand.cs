using Application.Abstractions.Messaging;
using Domain.Employees;
using SharedKernel.Result;

namespace Application.Employees.Update;

public sealed record UpdateEmployeeCommand(
    Guid Id,
    string FirstName,
    string LastName,
    RoleEnum Role,
    decimal Salary,
    SalaryCycleEnum SalaryCycle,
    bool IsActive) : ICommand<Updated>;
