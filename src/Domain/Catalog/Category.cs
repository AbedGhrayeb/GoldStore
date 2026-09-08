// <copyright file="Category.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Common;
using SharedKernel;
using SharedKernel.Result;

namespace Domain.Catalog;

public sealed class Category : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    public Guid? ParentCategoryId { get; set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public decimal? WeightInGrams { get; private set; }

    public Karat? Karat { get; private set; }

    public Category ParentCategory { get; set; }

    public ICollection<Category> Childrens { get; set; } = [];

    public Category()
    {
    }

    public Category(Guid id, Guid? parentCategoryId, string name, string? description, decimal? weightInGrams = null, Karat? karat = null)
        : base(id)
    {
        this.ParentCategoryId = parentCategoryId;
        this.Name = name;
        this.Description = description;
        this.WeightInGrams = weightInGrams;
        this.Karat = karat;
    }

    public static Result<Category> Create(Guid? parentCategoryId, string name, string? description, decimal? weightInGrams = null, Karat? karat = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return CategoryErrors.NameRequired;
        }

        if (weightInGrams.HasValue && weightInGrams.Value <= 0)
        {
            return CategoryErrors.InvalidWeight;
        }

        return new Category(Guid.CreateVersion7(), parentCategoryId, name, description, weightInGrams, karat);
    }

    public Result<Updated> Update(Guid? parentCategoryId, string name, string? description, bool isActive, decimal? weightInGrams = null, Karat? karat = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return CategoryErrors.NameRequired;
        }

        if (weightInGrams.HasValue && weightInGrams.Value <= 0)
        {
            return CategoryErrors.InvalidWeight;
        }

        this.ParentCategoryId = parentCategoryId;
        this.Name = name;
        this.Description = description;
        this.WeightInGrams = weightInGrams;
        this.Karat = karat;
        this.IsActive = isActive;
        return Result.Updated;
    }

    /// <summary>
    /// Direct stock movement on the category weight only. It intentionally does NOT
    /// post to the general gold ledger — callers (supplier delivery / scrap payment /
    /// sales invoice / customer purchase invoice) already post exactly one
    /// <see cref="Inventory.GoldLedgerEntry"/> for the store, so posting again here
    /// would double-count. Use the category create/update handlers when a ledger
    /// posting is actually wanted.
    /// </summary>
    /// <returns></returns>
    public Result<Updated> IncreaseWeight(decimal weightInGrams)
    {
        if (weightInGrams <= 0)
        {
            return CategoryErrors.InvalidWeight;
        }

        this.WeightInGrams = (this.WeightInGrams ?? 0M) + weightInGrams;
        return Result.Updated;
    }

    public Result<Updated> DecreaseWeight(decimal weightInGrams)
    {
        if (weightInGrams <= 0)
        {
            return CategoryErrors.InvalidWeight;
        }

        decimal current = this.WeightInGrams ?? 0M;
        if (weightInGrams > current)
        {
            return CategoryErrors.InsufficientWeight(current, weightInGrams);
        }

        this.WeightInGrams = current - weightInGrams;
        return Result.Updated;
    }
}
