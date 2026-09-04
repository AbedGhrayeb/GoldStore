using Application.Abstractions.Messaging;

namespace Application.Features.Users.TwoFactor;

public sealed record ResetPasswordWithPhoneCommand(string EmailOrPhone, string IdToken, string NewPassword) : ICommand<Guid>;
