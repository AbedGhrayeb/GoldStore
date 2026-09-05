// <copyright file="CreateSupplierCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Suppliers.Create;

public sealed record CreateSupplierCommand(
    string Name,
    string PrimaryPhone,
    string? SecondaryPhone,
    string? BankAccountNumber,
    string? Notes)
    : ICommand<Guid>;
