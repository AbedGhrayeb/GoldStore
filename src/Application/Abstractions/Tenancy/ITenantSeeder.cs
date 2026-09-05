// <copyright file="ITenantSeeder.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Abstractions.Tenancy;

/// <summary>
/// Seeds a freshly provisioned tenant schema (admin user, default financial accounts).
/// Idempotent — existing seed data is left untouched.
/// </summary>
public interface ITenantSeeder
{
    Task SeedAsync(
        TenantInfo tenant,
        string adminEmail,
        string adminPassword,
        string adminFirstName,
        string adminLastName,
        CancellationToken cancellationToken = default);
}
