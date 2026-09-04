using Application.Abstractions.Messaging;

namespace Application.Features.Platform.Auth.PlatformAdminLogin;

public sealed record PlatformAdminLoginCommand(string Email, string Password) : ICommand<PlatformAdminLoginResponse>;
