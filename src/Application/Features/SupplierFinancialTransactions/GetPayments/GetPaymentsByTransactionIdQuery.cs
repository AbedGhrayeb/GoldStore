// <copyright file="GetPaymentsByTransactionIdQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Features.SupplierFinancialTransactions.GetPayments;

public sealed record GetPaymentsByTransactionIdQuery(Guid TransactionId) : IQuery<List<SupplierFinancialPaymentResponse>>;
