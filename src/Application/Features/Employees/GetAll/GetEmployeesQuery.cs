using Application.Abstractions.Messaging;

namespace Application.Employees.GetAll;

public sealed record GetEmployeesQuery : IQuery<List<EmployeeResponse>>;
