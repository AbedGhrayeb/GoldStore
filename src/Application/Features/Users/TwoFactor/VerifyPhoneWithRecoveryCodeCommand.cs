// <copyright file="VerifyPhoneWithRecoveryCodeCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.Users.TwoFactor;

public sealed record VerifyPhoneWithRecoveryCodeCommand(string TempTicket, string RecoveryCode) : ICommand<Guid>;
