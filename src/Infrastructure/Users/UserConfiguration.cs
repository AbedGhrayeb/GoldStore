// <copyright file="UserConfiguration.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Users;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.SecurityStamp).HasMaxLength(128).IsRequired();
        builder.Property(u => u.PhoneNumber).HasMaxLength(30);
        builder.Property(u => u.WhatsappNumber).HasMaxLength(30);
        builder.Property(u => u.TwoFactorEnabled).IsRequired();
        builder.Property(u => u.PhoneNumberVerified).IsRequired();
        builder.Property(u => u.TwoFactorEnabledAtUtc);

        builder.HasIndex(u => u.Email).IsUnique();
    }
}
