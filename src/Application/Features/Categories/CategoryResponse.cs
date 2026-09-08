// <copyright file="CategoryResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Categories;

public sealed record CategoryResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public Guid? ParentCategoryId { get; init; }

    public string? ParentCategoryName { get; init; }

    public bool IsActive { get; init; }

    public decimal? WeightInGrams { get; init; }

    public int? Karat { get; init; }
}
