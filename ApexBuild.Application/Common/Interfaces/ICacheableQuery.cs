namespace ApexBuild.Application.Common.Interfaces;

/// <summary>
/// Marker interface. Apply to any MediatR query record/class that should be cached.
/// The <see cref="CachingBehaviour{TRequest,TResponse}"/> pipeline behavior picks
/// this up automatically — no other registration is needed per-query.
/// </summary>
public interface ICacheableQuery
{
    /// <summary>
    /// Unique, deterministic key for this query result.
    /// Convention: "{resource}:{scope}:{discriminators}".
    /// Example: "projects:org:abc123:user:def456:page:1".
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// How long the cached value is valid. Null → use the configured default TTL.
    /// </summary>
    TimeSpan? CacheDuration { get; }
}
