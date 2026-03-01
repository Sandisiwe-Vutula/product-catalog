using Microsoft.Extensions.Logging.Abstractions;
using ProductCatalog.API.DTOs;
using ProductCatalog.API.Services;
using Xunit;

namespace ProductCatalog.Tests
{
    /// <summary>
    /// Tests for SearchCacheService — verifies TTL expiry, key isolation,
    /// invalidation
    /// </summary>
    public class SearchCacheServiceTests
    {
        private static SearchCacheService MakeCache(TimeSpan? ttl = null) =>
            new(NullLogger<SearchCacheService>.Instance, ttl ?? TimeSpan.FromMinutes(5));

        private static ProductQueryDto Query(string? search = null, int? catId = null,
            decimal? min = null, decimal? max = null) =>
            new(search, catId, min, max, null);

        private static PagedResultDto<ProductDto> EmptyPage() =>
            new(new List<ProductDto>(), 0, 1, 10, 0);

        // ── TTL / Expiry ───────────────────────────────────────────────────────────

        [Fact]
        public async Task TryGet_AfterTtlExpired_ReturnsNull()
        {
            var cache = MakeCache(ttl: TimeSpan.FromMilliseconds(50));
            var query = Query("expires");

            cache.Set(query, EmptyPage());
            await Task.Delay(100); // wait for TTL to lapse

            Assert.Null(cache.TryGet(query));
        }

        [Fact]
        public void TryGet_BeforeTtlExpired_ReturnsCachedResult()
        {
            var cache = MakeCache(ttl: TimeSpan.FromSeconds(60));
            var query = Query("fresh");
            var result = EmptyPage();

            cache.Set(query, result);

            Assert.NotNull(cache.TryGet(query));
        }

        // ── Invalidation ───────────────────────────────────────────────────────────

        [Fact]
        public void Invalidate_ClearsAllEntries()
        {
            var cache = MakeCache();

            cache.Set(Query("a"), EmptyPage());
            cache.Set(Query("b"), EmptyPage());
            cache.Set(Query("c"), EmptyPage());

            cache.Invalidate();

            Assert.Null(cache.TryGet(Query("a")));
            Assert.Null(cache.TryGet(Query("b")));
            Assert.Null(cache.TryGet(Query("c")));
        }

        // ── Key isolation ──────────────────────────────────────────────────────────

        [Fact]
        public void TryGet_DifferentSearchTerms_AreIsolated()
        {
            var cache = MakeCache();
            var laptops = EmptyPage();
            var mice = EmptyPage();

            cache.Set(Query("laptop"), laptops);
            cache.Set(Query("mouse"), mice);

            var r1 = cache.TryGet(Query("laptop"));
            var r2 = cache.TryGet(Query("mouse"));

            Assert.Same(laptops, r1);
            Assert.Same(mice, r2);
            Assert.NotSame(r1, r2);
        }

        [Fact]
        public void TryGet_SameSearchDifferentPage_AreIsolated()
        {
            var cache = MakeCache();
            var page1 = new PagedResultDto<ProductDto>(new List<ProductDto>(), 30, 1, 10, 3);
            var page2 = new PagedResultDto<ProductDto>(new List<ProductDto>(), 30, 2, 10, 3);

            var q1 = new ProductQueryDto("laptop", null, null, null, null, "name", false, 1, 10);
            var q2 = new ProductQueryDto("laptop", null, null, null, null, "name", false, 2, 10);

            cache.Set(q1, page1);
            cache.Set(q2, page2);

            Assert.Same(page1, cache.TryGet(q1));
            Assert.Same(page2, cache.TryGet(q2));
        }

        [Fact]
        public void TryGet_PriceRangeQueries_AreIsolated()
        {
            var cache = MakeCache();
            var cheap = EmptyPage();
            var expensive = EmptyPage();

            cache.Set(Query(min: 0m, max: 100m), cheap);
            cache.Set(Query(min: 100m, max: 1000m), expensive);

            Assert.Same(cheap, cache.TryGet(Query(min: 0m, max: 100m)));
            Assert.Same(expensive, cache.TryGet(Query(min: 100m, max: 1000m)));
        }
    }
}
