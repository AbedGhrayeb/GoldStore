// <copyright file="GetSalesInvoiceByIdQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.SalesInvoices.GetById;

public sealed record GetSalesInvoiceByIdQuery(Guid Id) : IQuery<SalesInvoiceResponse>;
