// <copyright file="VerifyPlatformPhoneTwoFactorCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.PlatformUsers.TwoFactor;

public sealed record VerifyPlatformPhoneTwoFactorCommand(string TempTicket, string IdToken) : ICommand<Guid>;

public sealed record VerifyPlatformWithRecoveryCodeCommand(string TempTicket, string RecoveryCode) : ICommand<Guid>;
