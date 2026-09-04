using Application.Abstractions.Messaging;

namespace Application.Features.PlatformUsers.TwoFactor;

public sealed record SetupPlatformPhoneTwoFactorCommand(Guid PlatformUserId, string IdToken) : ICommand<SetupPlatformPhoneTwoFactorResult>;
public sealed record SetupPlatformPhoneTwoFactorResult(string PhoneNumber, IReadOnlyList<string> RecoveryCodes);
