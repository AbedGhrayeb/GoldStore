// <copyright file="ITenantContext.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Tenants;

namespace Application.Abstractions.Tenancy;

/// <summary>
/// The tenant resolved for the current request (or background scope).
/// Members throw when accessed while no tenant is resolved — check <see cref="IsResolved"/> first.
/// </summary>
public interface ITenantContext
{
    bool IsResolved { get; }

    bool IsReadOnly { get; }

    Guid TenantId { get; }

    string Subdomain { get; }

    string SchemaName { get; }

    TenantStatus Status { get; }

    /// <summary>Gets optional dedicated-database connection string (escape hatch). Null = shared database.</summary>
    string? ConnectionString { get; }
}
