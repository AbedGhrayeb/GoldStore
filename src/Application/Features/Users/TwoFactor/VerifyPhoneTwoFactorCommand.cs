// <copyright file="VerifyPhoneTwoFactorCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.Users.TwoFactor;

public sealed record VerifyPhoneTwoFactorCommand(string TempTicket, string IdToken) : ICommand<Guid>;
