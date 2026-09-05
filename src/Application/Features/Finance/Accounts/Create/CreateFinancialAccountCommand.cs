// <copyright file="CreateFinancialAccountCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Finance.Accounts.Create;

public sealed record CreateFinancialAccountCommand(
    string Name,
    string Currency,
    string? AccountNumber,
    string? Notes,
    decimal OpeningBalance = 0m) : ICommand<Guid>;
