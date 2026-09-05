// <copyright file="CreateSupplierManufacturingPaymentCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.Common.Ledger;

namespace Application.SupplierPayments.Manufacturing.Create;

public sealed record CreateSupplierManufacturingPaymentCommand(
    Guid SupplierId,
    Guid AccountId,
    decimal Amount,
    string Currency,
    string? Notes,
    List<PaymentLegDto>? PaymentLegs)
    : ICommand<Guid>;
