using ProductCatalog.API.DTOs;
using ProductCatalog.API.Interfaces;
using System.Collections.Concurrent;

namespace ProductCatalog.API.Services
{

    /// <summary>
    /// In-memory search result cache backed by a Dictionary.
    /// </summary>
    public class SearchCacheService : ISearchCacheService
    {
        // CacheEntry wraps the value with an expiry timestamp
        private record CacheEntry<T>(T Value, DateTime ExpiresAt);

        private readonly ConcurrentDictionary<string, CacheEntry<PagedResultDto<ProductDto>>> _cache = new();
        private readonly TimeSpan _ttl;
        private readonly ILogger<SearchCacheService> _logger;

        public SearchCacheService(ILogger<SearchCacheService> logger, TimeSpan? ttl = null)
        {
            _logger = logger;
            _ttl = ttl ?? TimeSpan.FromSeconds(30);
        }

        /// <summary>Try to get a cached result. Returns null on cache miss or expiry.</summary>
        public PagedResultDto<ProductDto>? TryGet(ProductQueryDto query)
        {
            var key = BuildKey(query);
            if (!_cache.TryGetValue(key, out var entry)) return null;

            if (entry.ExpiresAt < DateTime.UtcNow)
            {
                _cache.TryRemove(key, out _); // Lazy eviction
                _logger.LogDebug("Cache MISS (expired) for key: {Key}", key);
                return null;
            }

            _logger.LogDebug("Cache HIT for key: {Key}", key);
            return entry.Value;
        }

        /// <summary>Store a result in the cache.</summary>
        public void Set(ProductQueryDto query, PagedResultDto<ProductDto> result)
        {
            var key = BuildKey(query);
            var entry = new CacheEntry<PagedResultDto<ProductDto>>(result, DateTime.UtcNow.Add(_ttl));
            _cache[key] = entry;
            _logger.LogDebug("Cache SET for key: {Key}", key);
        }

        /// <summary>Invalidate all cached search results (call after any product mutation).</summary>
        public void Invalidate()
        {
            _cache.Clear();
            _logger.LogDebug("Cache invalidated.");
        }

        /// <summary>
        /// Builds a string key from all query parameters.
        /// </summary>
        private static string BuildKey(ProductQueryDto q) =>
            $"s={q.Search}|c={q.CategoryId}|min={q.MinPrice}|max={q.MaxPrice}|stock={q.InStock}" +
            $"|sort={q.SortBy}|desc={q.SortDesc}|p={q.Page}|ps={q.PageSize}";
    }

}
