using Application.Abstractions.Messaging;
using Domain.Employees;

namespace Application.Users.Create;

public sealed record CreateUserCommand(string Email, string FirstName, string LastName, string Password,RoleEnum Role)
    : ICommand<Guid>;
