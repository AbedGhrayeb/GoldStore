using Application.Abstractions.Messaging;

namespace Application.Features.Users.TwoFactor;

public sealed record SetupPhoneTwoFactorCommand(Guid UserId, string IdToken) : ICommand<SetupPhoneTwoFactorResult>;

public sealed record SetupPhoneTwoFactorResult(string PhoneNumber, IReadOnlyList<string> RecoveryCodes);
