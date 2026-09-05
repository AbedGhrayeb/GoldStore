// <copyright file="SalesInvoiceResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Features.SalesInvoices;

public sealed record SalesInvoiceResponse
{
    public Guid Id { get; init; }

    public string InvoiceNumber { get; init; } = string.Empty;

    public string CustomerName { get; init; } = string.Empty;

    public string? CustomerPhone { get; init; }

    public DateTime Date { get; init; }

    public string Currency { get; init; } = string.Empty;

    public decimal TotalAmount { get; init; }

    public decimal AmountPaid { get; init; }

    public decimal RemainingBalance { get; init; }

    public string? PaymentMethod { get; init; }

    public string Status { get; init; } = string.Empty;

    public string StatusLabel { get; init; } = string.Empty;

    public string UserName { get; init; } = string.Empty;

    public string? Notes { get; init; }

    public DateTime CreatedAt { get; init; }

    public List<SalesInvoiceItemResponse> Items { get; init; } = [];
}
