// <copyright file="SetAccountBalanceCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using SharedKernel.Result;

namespace Application.Finance.Accounts.SetBalance;

public sealed record SetAccountBalanceCommand(
    Guid AccountId,
    decimal TargetBalance,
    string? Notes) : ICommand<Updated>;
