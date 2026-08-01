using Application.Abstractions.Messaging;
using Domain.Employees;

namespace Application.Employees.Create;

public sealed record CreateEmployeeCommand(
    string FirstName,
    string LastName,
    RoleEnum Role,
    decimal Salary,
    SalaryCycleEnum SalaryCycle,
    bool ConnectToUser,
    Guid? ExistingUserId,
    string? NewUserEmail,
    string? NewUserPassword) : ICommand<Guid>;
