// <copyright file="ResetPlatformPasswordWithPhoneCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.PlatformUsers.TwoFactor;

public sealed record ResetPlatformPasswordWithPhoneCommand(string EmailOrPhone, string IdToken, string NewPassword) : ICommand<Guid>;
