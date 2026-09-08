// <copyright file="CreateCustomerPurchaseInvoiceCommand.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;
using Application.Common.Ledger;

namespace Application.Features.CustomerPurchaseInvoices.Create;

public sealed record CreateCustomerPurchaseInvoiceCommand(
    string SellerName,
    string? SellerPhone,
    string? SellerIdNumber,
    int? SellerYearOfBirth,
    string? SellerAddress,
    Guid EmployeeId,
    string Currency,
    DateTime Date,
    decimal TotalAmount,
    decimal AmountPaid,
    int PaymentMethod,
    Guid AccountId,
    string? SellerAccountNumber,
    string? Notes,
    List<CustomerPurchaseInvoiceItemDto> Items,
    List<PaymentLegDto>? PaymentLegs) : ICommand<Guid>;

public sealed record CustomerPurchaseInvoiceItemDto(
    Guid? CategoryId,
    int Karat,
    decimal WeightInGrams,
    decimal PricePerGram);
