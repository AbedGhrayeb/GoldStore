using Application.Abstractions.Messaging;

namespace Application.Features.Users.TwoFactor;

public sealed record GetPhoneTwoFactorStatusQuery(Guid UserId) : IQuery<PhoneTwoFactorStatusResponse>;

public sealed record PhoneTwoFactorStatusResponse(bool TwoFactorEnabled, bool PhoneNumberVerified, string? MaskedPhone, IReadOnlyList<string> UnusedRecoveryCodesCountPlaceholder);
