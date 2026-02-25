using System.Collections.Concurrent;
using System.Text.Json;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Infrastructure.Configurations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApexBuild.Infrastructure.Services;

/// <summary>
/// In-process cache backed by <see cref="IMemoryCache"/>.
/// A concurrent tracked-key set enables prefix-based bulk invalidation —
/// the key pattern used for group eviction after mutations.
///
/// Thread-safety: ConcurrentDictionary is used for the key registry so parallel
/// requests don't corrupt the tracked set.
/// </summary>
public sealed class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _defaultTtl;
    private readonly ILogger<CacheService> _logger;

    // Tracks every key ever written so we can do prefix scans.
    // ConcurrentDictionary<key, byte> acts as a thread-safe HashSet.
    private readonly ConcurrentDictionary<string, byte> _trackedKeys = new();

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public CacheService(
        IMemoryCache cache,
        IOptions<CacheSettings> options,
        ILogger<CacheService> logger)
    {
        _cache = cache;
        _defaultTtl = TimeSpan.FromMinutes(options.Value.DefaultTtlMinutes);
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var raw) && raw is string json)
        {
            try
            {
                var value = JsonSerializer.Deserialize<T>(json, _jsonOpts);
                return Task.FromResult(value);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Cache deserialization failed for key {Key}. Treating as MISS.", key);
                _ = RemoveAsync(key, cancellationToken);
            }
        }

        return Task.FromResult<T?>(default);
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, TimeSpan? duration = null, CancellationToken cancellationToken = default)
    {
        if (value is null) return Task.CompletedTask;

        var ttl = duration ?? _defaultTtl;
        var json = JsonSerializer.Serialize(value, _jsonOpts);

        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(ttl)
            // Remove from tracked keys when evicted so the registry stays clean
            .RegisterPostEvictionCallback((evictedKey, _, _, _) =>
                _trackedKeys.TryRemove(evictedKey.ToString()!, out _));

        _cache.Set(key, json, options);
        _trackedKeys.TryAdd(key, 0);

        _logger.LogDebug("Cache SET  {Key} (TTL {Ttl})", key, ttl);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);
        _trackedKeys.TryRemove(key, out _);
        _logger.LogDebug("Cache DEL  {Key}", key);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        // Snapshot keys first so we don't modify the dictionary while iterating
        var toRemove = _trackedKeys.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in toRemove)
        {
            _cache.Remove(key);
            _trackedKeys.TryRemove(key, out _);
        }

        if (toRemove.Count > 0)
            _logger.LogDebug("Cache EVICT prefix={Prefix} count={Count}", prefix, toRemove.Count);

        return Task.CompletedTask;
    }
}
