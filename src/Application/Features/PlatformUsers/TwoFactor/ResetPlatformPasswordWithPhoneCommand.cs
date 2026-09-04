using Application.Abstractions.Messaging;

namespace Application.Features.PlatformUsers.TwoFactor;

public sealed record ResetPlatformPasswordWithPhoneCommand(string EmailOrPhone, string IdToken, string NewPassword) : ICommand<Guid>;
