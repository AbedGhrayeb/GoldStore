// <copyright file="GetCustomerPurchaseInvoiceByIdQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.CustomerPurchaseInvoices.GetById;

public sealed record GetCustomerPurchaseInvoiceByIdQuery(Guid Id) : IQuery<CustomerPurchaseInvoiceResponse>;
