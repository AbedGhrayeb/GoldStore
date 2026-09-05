// <copyright file="GetCustomerPurchaseInvoicesKpisQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.CustomerPurchaseInvoices.GetKpis;

public sealed record GetCustomerPurchaseInvoicesKpisQuery : IQuery<CustomerPurchaseInvoiceKpiResponse>;
