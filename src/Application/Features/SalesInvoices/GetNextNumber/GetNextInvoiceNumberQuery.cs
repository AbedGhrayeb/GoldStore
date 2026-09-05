// <copyright file="GetNextInvoiceNumberQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.SalesInvoices.GetNextNumber;

public sealed record GetNextInvoiceNumberQuery : IQuery<string>;
