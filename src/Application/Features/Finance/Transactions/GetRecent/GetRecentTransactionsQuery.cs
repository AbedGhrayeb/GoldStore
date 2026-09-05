// <copyright file="GetRecentTransactionsQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.Finance.Transactions;

namespace Application.Finance.Transactions.GetRecent;

public sealed record GetRecentTransactionsQuery(int Count = 20) : IQuery<List<RecentTransactionResponse>>;
