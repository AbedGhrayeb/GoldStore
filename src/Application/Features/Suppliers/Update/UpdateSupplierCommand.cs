// <copyright file="UpdateSupplierCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using SharedKernel.Result;

namespace Application.Suppliers.Update;

public sealed record UpdateSupplierCommand(
    Guid Id,
    string Name,
    string PrimaryPhone,
    string? SecondaryPhone,
    string? BankAccountNumber,
    string? Notes,
    bool IsActive)
    : ICommand<Updated>;
