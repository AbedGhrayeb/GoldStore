using Application.Abstractions.Messaging;
using SharedKernel.Result;

namespace Application.Employees.ToggleActive;

public sealed record ToggleActiveEmployeeCommand(Guid Id) : ICommand<Updated>;
