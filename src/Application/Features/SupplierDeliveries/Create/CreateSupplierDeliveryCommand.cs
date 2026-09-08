// <copyright file="CreateSupplierDeliveryCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.Common.Ledger;

namespace Application.SupplierDeliveries.Create;

public sealed record CreateSupplierDeliveryCommand(
    Guid SupplierId,
    List<DeliveryLineDto> Lines,
    decimal ManufacturingFeePerGram,
    string ManufacturingFeeCurrency,
    decimal AmountDue,
    string AmountDueCurrency,
    List<PaymentLegDto>? PaymentLegs,
    string? Notes)
    : ICommand<string>;
