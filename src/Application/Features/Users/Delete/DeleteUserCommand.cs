// <copyright file="DeleteUserCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Users.Delete;

public sealed record DeleteUserCommand(Guid Id) : ICommand<bool>;
