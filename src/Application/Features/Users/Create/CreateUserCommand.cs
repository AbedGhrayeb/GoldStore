// <copyright file="CreateUserCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Users.Create;

public sealed record CreateUserCommand(string Email, string FirstName, string LastName, string Password, string? PhoneNumber = null, string? WhatsappNumber = null)
    : ICommand<Guid>;
