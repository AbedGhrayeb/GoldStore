// <copyright file="UserRecoveryCodeConfiguration.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Users;

internal sealed class UserRecoveryCodeConfiguration : IEntityTypeConfiguration<UserRecoveryCode>
{
    public void Configure(EntityTypeBuilder<UserRecoveryCode> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CodeHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(e => new { e.UserId, e.CodeHash }).IsUnique();
        builder.HasIndex(e => e.TenantId);
    }
}
