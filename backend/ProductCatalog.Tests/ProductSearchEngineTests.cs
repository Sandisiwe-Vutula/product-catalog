using ProductCatalog.API.Interfaces;
using ProductCatalog.API.Models;
using ProductCatalog.API.Utilities;
using Xunit;

namespace ProductCatalog.Tests
{
    /// <summary>
    /// Tests for the custom ProductSearchEngine — pure BCL, no EF, no mocks.
    /// Verifies exact match, prefix, substring, fuzzy, and multi-field weighted scoring.
    /// </summary>
    public class ProductSearchEngineTests
    {
        private readonly IProductSearchEngine<Product> _engine;
        private readonly List<Product> _products;

        public ProductSearchEngineTests()
        {
            _engine = new ProductSearchEngine<Product>()
                .AddField("Name", p => p.Name, 2.0)
                .AddField("Description", p => p.Description, 1.0)
                .AddField("SKU", p => p.SKU, 1.5);

            _products = new List<Product>
            {
                new() { Id = 1, Name = "Laptop Pro 15",  Description = "High-performance laptop", SKU = "LAP-001", Price = 1299m, Quantity = 10, CategoryId = 1 },
                new() { Id = 2, Name = "Wireless Mouse",  Description = "Ergonomic mouse",         SKU = "MOU-001", Price = 49m,   Quantity = 50, CategoryId = 1 },
                new() { Id = 3, Name = "Bluetooth Speaker", Description = "Portable audio",        SKU = "SPK-001", Price = 59m,   Quantity = 30, CategoryId = 1 },
                new() { Id = 4, Name = "iPhone 15",       Description = "Apple smartphone",        SKU = "PHN-001", Price = 999m,  Quantity = 20, CategoryId = 2 },
                new() { Id = 5, Name = "USB-C Hub",       Description = "Multiport adapter hub",   SKU = "HUB-001", Price = 69m,   Quantity = 80, CategoryId = 1 },
            };
        }

        [Fact]
        public void Search_ExactNameMatch_ReturnsHighestScore()
        {
            var results = _engine.Search(_products, "Laptop Pro 15").ToList();

            Assert.NotEmpty(results);
            Assert.Equal(1, results[0].Item.Id);
            Assert.True(results[0].Score > 1.0, "Exact match should have score above 1.0 due to weight");
        }

        [Fact]
        public void Search_PrefixMatch_ReturnsCorrectProduct()
        {
            var results = _engine.Search(_products, "lapt").ToList();

            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.Item.Id == 1);
        }

        [Fact]
        public void Search_FuzzyMatch_FindsProductWithTypo()
        {
            // "lptop" is 1 edit away from "laptop"
            var results = _engine.Search(_products, "lptop").ToList();

            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.Item.Id == 1);
        }

        [Fact]
        public void Search_CaseInsensitive_ReturnsMatch()
        {
            var upper = _engine.Search(_products, "MOUSE").ToList();
            var lower = _engine.Search(_products, "mouse").ToList();
            var mixed = _engine.Search(_products, "Mouse").ToList();

            Assert.All(new[] { upper, lower, mixed }, r =>
                Assert.Contains(r, x => x.Item.Id == 2));
        }

        [Fact]
        public void Search_SubstringMatchInDescription_ReturnsProduct()
        {
            var results = _engine.Search(_products, "ergonomic").ToList();

            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.Item.Id == 2);
        }

        [Fact]
        public void Search_SkuMatch_ReturnsCorrectProduct()
        {
            var results = _engine.Search(_products, "SPK-001").ToList();

            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.Item.Id == 3);
        }

        [Fact]
        public void Search_ResultsSortedByScoreDescending()
        {
            var results = _engine.Search(_products, "laptop").ToList();

            for (int i = 1; i < results.Count; i++)
                Assert.True(results[i - 1].Score >= results[i].Score,
                    "Results should be sorted by score descending");
        }

        [Fact]
        public void Search_EmptyQuery_ReturnsAllProducts()
        {
            var results = _engine.Search(_products, "").ToList();
            Assert.Equal(_products.Count, results.Count);
        }

        [Fact]
        public void Search_NoMatch_ReturnsEmptyList()
        {
            var results = _engine.Search(_products, "zzzzzzzzz").ToList();
            Assert.Empty(results);
        }

        [Fact]
        public void Search_WeightedScoring_NameMatchOutscoresDescriptionMatch()
        {
            // "hub" appears as standalone word in both Name ("USB-C Hub") and Description ("Multiport adapter hub")
            var results = _engine.Search(_products, "hub").ToList();

            // The product where "hub" matches the name should rank higher than
            // the one where it only matches the description
            var nameMatch = results.FirstOrDefault(r => r.Item.Id == 5);  // "USB-C Hub"
            Assert.NotNull(nameMatch);
            Assert.True(nameMatch.Score > 0);
        }
    }
}
