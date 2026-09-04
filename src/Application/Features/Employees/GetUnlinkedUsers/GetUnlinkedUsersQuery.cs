using Application.Abstractions.Messaging;

namespace Application.Employees.GetUnlinkedUsers;

public sealed record GetUnlinkedUsersQuery : IQuery<List<EmployeeUserOptionResponse>>;
