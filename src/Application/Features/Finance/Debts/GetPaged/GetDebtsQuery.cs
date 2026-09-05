// <copyright file="GetDebtsQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.Common.Models;

namespace Application.Features.Finance.Debts.GetPaged;

public sealed record GetDebtsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Direction = null,
    string? Search = null) : IQuery<PaginatedList<DebtResponse>>;
