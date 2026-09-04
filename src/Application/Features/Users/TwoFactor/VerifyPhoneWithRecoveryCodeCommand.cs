using Application.Abstractions.Messaging;

namespace Application.Features.Users.TwoFactor;

public sealed record VerifyPhoneWithRecoveryCodeCommand(string TempTicket, string RecoveryCode) : ICommand<Guid>;
