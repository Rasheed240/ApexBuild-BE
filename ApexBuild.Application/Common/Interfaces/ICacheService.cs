namespace ApexBuild.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the in-process (IMemoryCache) cache.
/// Designed so the implementation can be swapped for an IDistributedCache/Redis
/// implementation later without changing any consuming code.
/// </summary>
public interface ICacheService
{
    /// <summary>Retrieve a value; returns null on MISS or expiry.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>Store a value with an optional duration (falls back to the configured default).</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? duration = null, CancellationToken cancellationToken = default);

    /// <summary>Remove a single key.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove every key that starts with <paramref name="prefix"/>.
    /// Used for group-invalidation after mutations (e.g. "projects:org:{id}:").
    /// </summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
