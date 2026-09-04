using Application.Abstractions.Messaging;

namespace Application.PlatformUsers.Login;

public sealed record LoginPlatformUserCommand(string Email, string Password) : ICommand<Guid>;
