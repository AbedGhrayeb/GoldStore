// <copyright file="GetNextCustomerPurchaseInvoiceNumberQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.CustomerPurchaseInvoices.GetNextNumber;

public sealed record GetNextCustomerPurchaseInvoiceNumberQuery : IQuery<string>;
