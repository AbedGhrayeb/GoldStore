using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Employees;
using SharedKernel.Result;

namespace Application.Employees.Update;

public sealed record UpdateEmployeeCommand(
    Guid Id,
    string FirstName,
    string LastName,
    RoleEnum Role,
    decimal Salary,
    Currency Currency,
    SalaryCycleEnum SalaryCycle,
    bool IsActive) : ICommand<Updated>;
