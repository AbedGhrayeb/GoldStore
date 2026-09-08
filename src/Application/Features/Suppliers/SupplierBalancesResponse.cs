// <copyright file="SupplierBalancesResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Suppliers;

public sealed record SupplierBalancesResponse
{
    public Guid SupplierId { get; init; }

    public List<SupplierGoldDue> GoldByKarat { get; init; } = [];

    public List<SupplierManufacturingDue> ManufacturingByCurrency { get; init; } = [];
}

public sealed record SupplierGoldDue
{
    public int Karat { get; init; }

    public decimal NetWeight { get; init; }
}

public sealed record SupplierManufacturingDue
{
    public string Currency { get; init; } = string.Empty;

    public decimal NetAmount { get; init; }
}
