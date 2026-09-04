using Application.Abstractions.Messaging;

namespace Application.Users.Delete;

public sealed record DeleteUserCommand(Guid Id) : ICommand<bool>;
