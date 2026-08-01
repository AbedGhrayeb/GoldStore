using Application.Abstractions.Messaging;

namespace Application.Employees.GetById;

public sealed record GetEmployeeByIdQuery(Guid Id) : IQuery<EmployeeResponse>;
