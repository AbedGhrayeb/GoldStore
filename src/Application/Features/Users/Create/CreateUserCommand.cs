using Application.Abstractions.Messaging;

namespace Application.Users.Create;

public sealed record CreateUserCommand(string Email, string FirstName, string LastName, string Password, string? PhoneNumber = null, string? WhatsappNumber = null)
    : ICommand<Guid>;
