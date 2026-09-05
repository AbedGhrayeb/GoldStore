// <copyright file="GetFinancialAccountsQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Finance.Accounts.GetAll;

public sealed record GetFinancialAccountsQuery(bool ActiveOnly = true) : IQuery<List<FinancialAccountResponse>>;
