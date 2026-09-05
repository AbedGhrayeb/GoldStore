// <copyright file="GetSupplierFinancialKpisQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.SupplierFinancialTransactions.GetKpis;

public sealed record GetSupplierFinancialKpisQuery : IQuery<SupplierFinancialKpiResponse>;
