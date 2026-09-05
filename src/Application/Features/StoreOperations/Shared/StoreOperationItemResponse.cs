// <copyright file="StoreOperationItemResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Features.StoreOperations.Shared;

public sealed record StoreOperationItemResponse
{
    public Guid Id { get; init; }

    public int Karat { get; init; }

    public decimal WeightInGrams { get; init; }

    public decimal Equivalent21KWeightInGrams { get; init; }

    public decimal PricePerGram { get; init; }

    public decimal GoldAmount { get; init; }

    public string? CategoryName { get; init; }
}
