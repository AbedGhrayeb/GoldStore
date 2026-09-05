// <copyright file="GetDebtKpisQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.Finance.Debts.GetKpis;

public sealed record GetDebtKpisQuery : IQuery<DebtKpiResponse>;
