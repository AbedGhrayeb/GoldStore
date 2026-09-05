// <copyright file="HybridCacheService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Hybrid;

namespace Infrastructure.Caching;

/// <summary>
/// Process-wide cache abstraction backed by HybridCache (M7-C1). HybridCache's per-tag
/// <c>RemoveByTagAsync</c> does not invalidate in-memory (L1) entries when no distributed
/// backend is configured — only key-based removal and the wildcard tag work (verified
/// against Microsoft.Extensions.Caching.Hybrid 9.x and 10.x). Tag eviction is therefore
/// implemented here as tracked key-based removal: every tagged <c>GetOrCreateAsync</c>
/// registers its key under its tags, and <c>RemoveByTagAsync</c> removes each registered
/// key directly, which HybridCache honours. Keys are bounded (one per KPI page per tenant).
/// </summary>
internal sealed class HybridCacheService(HybridCache cache) : ICacheService
{
    private readonly object gate = new();
    private readonly Dictionary<string, HashSet<string>> keysByTag = new(StringComparer.Ordinal);

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        IReadOnlyList<string> tags,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan expiration,
        CancellationToken cancellationToken)
    {
        this.Register(key, tags);

        HybridCacheEntryOptions options = new()
        {
            Expiration = expiration,
            LocalCacheExpiration = expiration,
        };

        return await cache.GetOrCreateAsync<T>(
            key,
            (token) => new ValueTask<T>(factory(token)),
            options,
            tags,
            cancellationToken);
    }

    public async Task RemoveByTagAsync(string tag, CancellationToken cancellationToken)
    {
        HashSet<string>? keys;
        lock (this.gate)
        {
            this.keysByTag.TryGetValue(tag, out keys);
        }

        if (keys is null)
        {
            return;
        }

        foreach (string key in keys)
        {
            await cache.RemoveAsync(key, cancellationToken);
        }
    }

    private void Register(string key, IReadOnlyList<string> tags)
    {
        if (tags.Count == 0)
        {
            return;
        }

        lock (this.gate)
        {
            foreach (string tag in tags)
            {
                if (!this.keysByTag.TryGetValue(tag, out HashSet<string>? keys))
                {
                    keys = new HashSet<string>(StringComparer.Ordinal);
                    this.keysByTag.Add(tag, keys);
                }

                keys.Add(key);
            }
        }
    }
}
