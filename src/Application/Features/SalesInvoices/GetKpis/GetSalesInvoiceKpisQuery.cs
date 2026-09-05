// <copyright file="GetSalesInvoiceKpisQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.SalesInvoices.GetKpis;

public sealed record GetSalesInvoiceKpisQuery : IQuery<SalesInvoiceKpiResponse>;
