using ApexBuild.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ApexBuild.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behavior that transparently caches query responses.
///
/// Only activates when the incoming request implements <see cref="ICacheableQuery"/>.
/// All other requests (commands, non-cacheable queries) pass straight through.
///
/// Flow:
///   1. Request implements ICacheableQuery → compute CacheKey
///   2. Cache HIT  → return immediately (zero DB hits)
///   3. Cache MISS → call next handler → store result → return
/// </summary>
public sealed class CachingBehaviour<TRequest, TResponse>(
    ICacheService cacheService,
    ILogger<CachingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only cache requests that explicitly opt in via ICacheableQuery
        if (request is not ICacheableQuery cacheableQuery)
            return await next();

        var key = cacheableQuery.CacheKey;

        var cached = await cacheService.GetAsync<TResponse>(key, cancellationToken);
        if (cached is not null)
        {
            logger.LogDebug("Cache HIT  {RequestType} key={Key}", typeof(TRequest).Name, key);
            return cached;
        }

        logger.LogDebug("Cache MISS {RequestType} key={Key}", typeof(TRequest).Name, key);

        var response = await next();

        if (response is not null)
        {
            await cacheService.SetAsync(key, response, cacheableQuery.CacheDuration, cancellationToken);
        }

        return response;
    }
}
