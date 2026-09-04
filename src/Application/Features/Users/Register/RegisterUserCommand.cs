using Application.Abstractions.Messaging;
using Domain.Employees;

namespace Application.Users.Register;

public sealed record RegisterUserCommand(string Email, string FirstName, string LastName, string Password,RoleEnum Role)
    : ICommand<Guid>;
