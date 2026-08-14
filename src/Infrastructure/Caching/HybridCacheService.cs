using Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Hybrid;

namespace Infrastructure.Caching;

internal sealed class HybridCacheService(HybridCache cache) : ICacheService
{
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        IReadOnlyList<string> tags,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan expiration,
        CancellationToken cancellationToken)
    {
        HybridCacheEntryOptions options = new()
        {
            Expiration = expiration,
            LocalCacheExpiration = expiration
        };

        return await cache.GetOrCreateAsync<T>(
            key,
            (token) => new ValueTask<T>(factory(token)),
            options,
            tags,
            cancellationToken);
    }

    public async Task RemoveByTagAsync(string tag, CancellationToken cancellationToken) =>
        await cache.RemoveByTagAsync(tag, cancellationToken);
}
