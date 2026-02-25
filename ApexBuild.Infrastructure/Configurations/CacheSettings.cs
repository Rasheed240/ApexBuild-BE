namespace ApexBuild.Infrastructure.Configurations;

/// <summary>
/// Bound from the "Cache" section in appsettings.json.
/// </summary>
public sealed class CacheSettings
{
    public const string SectionName = "Cache";

    /// <summary>Default TTL (minutes) applied when a query does not specify its own duration.</summary>
    public int DefaultTtlMinutes { get; set; } = 5;

    /// <summary>
    /// Hard memory limit (MB) for the in-process cache.
    /// Prevents runaway memory consumption on small Render dynos.
    /// </summary>
    public long MaxMemoryMB { get; set; } = 128;
}
