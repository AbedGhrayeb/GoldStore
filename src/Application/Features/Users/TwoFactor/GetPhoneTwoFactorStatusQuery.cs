// <copyright file="GetPhoneTwoFactorStatusQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.Users.TwoFactor;

public sealed record GetPhoneTwoFactorStatusQuery(Guid UserId) : IQuery<PhoneTwoFactorStatusResponse>;

public sealed record PhoneTwoFactorStatusResponse(bool TwoFactorEnabled, bool PhoneNumberVerified, string? MaskedPhone, IReadOnlyList<string> UnusedRecoveryCodesCountPlaceholder);
