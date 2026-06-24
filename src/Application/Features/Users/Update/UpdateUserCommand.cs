using Application.Abstractions.Messaging;

namespace Application.Users.Update;

public sealed record UpdateUserCommand(Guid Id, string FirstName, string LastName, string? Password = null)
    : ICommand<bool>;
