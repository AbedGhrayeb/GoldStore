// <copyright file="SubscriptionPlanConfiguration.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Tenants;

internal sealed class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.HasKey(plan => plan.Id);
        builder.Property(plan => plan.Name).HasMaxLength(100).IsRequired();
        builder.Property(plan => plan.Key).HasMaxLength(63).IsRequired();
        builder.HasIndex(plan => plan.Key).IsUnique();
        builder.Property(plan => plan.IsTrial).IsRequired();
        builder.Property(plan => plan.DurationInMonths).IsRequired();
        builder.Property(plan => plan.Price).HasPrecision(18, 2).IsRequired();
        builder.Property(plan => plan.DiscountPercent).HasPrecision(5, 2);
    }
}
