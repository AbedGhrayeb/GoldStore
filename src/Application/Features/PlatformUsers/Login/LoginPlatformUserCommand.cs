// <copyright file="LoginPlatformUserCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.PlatformUsers.Login;

public sealed record LoginPlatformUserCommand(string Email, string Password) : ICommand<Guid>;
