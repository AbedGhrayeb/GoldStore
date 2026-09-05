// <copyright file="Plan.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Common;
using SharedKernel;
using SharedKernel.Result;

namespace Domain.Tenants;

public sealed class Plan : Entity
{
    public string Name { get; private set; }

    public string? Description { get; private set; }

    public decimal MonthlyPrice { get; private set; }

    public decimal AnnualPrice { get; private set; }

    public Currency Currency { get; private set; }

    public string? FeaturesJson { get; private set; }

    private Plan()
    {
    }

    private Plan(Guid id, string name, string? description, decimal monthlyPrice, decimal annualPrice, Currency currency, string? featuresJson)
        : base(id)
    {
        this.Name = name;
        this.Description = description;
        this.MonthlyPrice = monthlyPrice;
        this.AnnualPrice = annualPrice;
        this.Currency = currency;
        this.FeaturesJson = featuresJson;
    }

    public static Result<Plan> Create(string name, decimal monthlyPrice, decimal annualPrice, Currency currency, string? description = null, string? featuresJson = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return PlanErrors.NameRequired;
        }

        if (monthlyPrice < 0 || annualPrice < 0)
        {
            return PlanErrors.InvalidPrice;
        }

        return new Plan(Guid.CreateVersion7(), name, description, monthlyPrice, annualPrice, currency, featuresJson);
    }

    public Result<Updated> Update(string name, string? description, decimal monthlyPrice, decimal annualPrice, string? featuresJson)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return PlanErrors.NameRequired;
        }

        if (monthlyPrice < 0 || annualPrice < 0)
        {
            return PlanErrors.InvalidPrice;
        }

        this.Name = name;
        this.Description = description;
        this.MonthlyPrice = monthlyPrice;
        this.AnnualPrice = annualPrice;
        this.FeaturesJson = featuresJson;

        return Result.Updated;
    }
}
