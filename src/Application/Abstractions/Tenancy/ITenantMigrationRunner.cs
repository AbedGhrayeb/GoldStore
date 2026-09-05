// <copyright file="ITenantMigrationRunner.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using SharedKernel.Result;

namespace Application.Abstractions.Tenancy;

/// <summary>
/// Brings every active tenant schema up to date at startup by replaying the
/// idempotent tenant migration script, and records the outcome in the platform
/// migration log. Also adopts legacy schemas that predate per-tenant history.
/// </summary>
public interface ITenantMigrationRunner
{
    Task<Success> RunAsync(CancellationToken cancellationToken = default);
}
