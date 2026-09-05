// <copyright file="UpdateUserCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Users.Update;

public sealed record UpdateUserCommand(Guid Id, string FirstName, string LastName, string? Password = null, string? PhoneNumber = null, string? WhatsappNumber = null)
    : ICommand<bool>;
