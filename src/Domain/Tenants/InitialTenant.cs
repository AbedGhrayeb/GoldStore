// <copyright file="InitialTenant.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Domain.Tenants;

/// <summary>
/// Well-known identity of the initial tenant representing the original single-store
/// GoldStore deployment. Existing rows are backfilled with this tenant, and the interim
/// write-stamping assigns it until Phase 2/3 introduce ICurrentTenant and the write guard.
/// </summary>
public static class InitialTenant
{
    public static Guid Id { get; } = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public const string Key = "goldstore";

    public const string Name = "GoldStore";
}
