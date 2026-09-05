// <copyright file="GetAccountBalanceQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Finance.Accounts.GetBalance;

public sealed record GetAccountBalanceQuery(Guid AccountId) : IQuery<AccountBalanceResponse>;
