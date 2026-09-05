// <copyright file="ICacheService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Abstractions.Caching;

/// <summary>
/// Process-wide cache abstraction (backed by HybridCache — M7-C1). Keys and tags are
/// tenant-scoped by the caller; the single-tenant store keeps balances and KPIs correct
/// by evicting a tenant's tag on any of its ledger writes.
/// </summary>
public interface ICacheService
{
    Task<T> GetOrCreateAsync<T>(
        string key,
        IReadOnlyList<string> tags,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);

    Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);
}
