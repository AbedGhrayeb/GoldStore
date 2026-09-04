using Application.Abstractions.Messaging;

namespace Application.Features.PlatformUsers.TwoFactor;

public sealed record VerifyPlatformPhoneTwoFactorCommand(string TempTicket, string IdToken) : ICommand<Guid>;
public sealed record VerifyPlatformWithRecoveryCodeCommand(string TempTicket, string RecoveryCode) : ICommand<Guid>;
