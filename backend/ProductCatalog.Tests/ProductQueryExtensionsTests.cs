using ProductCatalog.API.Extensions;
using ProductCatalog.API.Models;
using Xunit;

namespace ProductCatalog.Tests
{
    /// <summary>
    /// Tests for ProductQueryExtensions custom LINQ methods.
    /// Uses in-memory IQueryable so no EF or database is needed.
    /// </summary>
    public class ProductQueryExtensionsTests
    {
        private static IQueryable<Product> MakeProducts() => new List<Product>
        {
            new() { Id = 1, Name = "Laptop Pro 15",   Description = "High-performance laptop", SKU = "LAP-001", Price = 1299m, Quantity = 15, CategoryId = 2 },
            new() { Id = 2, Name = "Wireless Mouse",   Description = "Ergonomic mouse",         SKU = "MOU-001", Price = 49m,   Quantity = 0,  CategoryId = 2 },
            new() { Id = 3, Name = "iPhone 15",        Description = "Apple smartphone",         SKU = "PHN-001", Price = 999m,  Quantity = 30, CategoryId = 3 },
            new() { Id = 4, Name = "Classic T-Shirt",  Description = "Cotton t-shirt",           SKU = "MEN-001", Price = 19m,   Quantity = 100,CategoryId = 5 },
            new() { Id = 5, Name = "Gaming Desktop X", Description = "High-end gaming PC",       SKU = "DESK-001",Price = 1899m, Quantity = 8,  CategoryId = 2 },
        }.AsQueryable();

        // ── FilterBySearch ─────────────────────────────────────────────────────────

        [Theory]
        [InlineData("laptop", 1)]
        [InlineData("LAP-001", 1)]   // SKU match
        [InlineData("mouse", 1)]
        [InlineData("shirt", 1)]
        [InlineData("gaming", 1)]
        public void FilterBySearch_MatchingTerm_ReturnsExpectedCount(string term, int expected)
        {
            var result = MakeProducts().FilterBySearch(term).ToList();
            Assert.Equal(expected, result.Count);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void FilterBySearch_NullOrEmpty_ReturnsAllProducts(string? term)
        {
            var result = MakeProducts().FilterBySearch(term).ToList();
            Assert.Equal(5, result.Count);
        }

        [Fact]
        public void FilterBySearch_CaseInsensitive_Matches()
        {
            var upper = MakeProducts().FilterBySearch("LAPTOP").ToList();
            var lower = MakeProducts().FilterBySearch("laptop").ToList();

            Assert.Equal(upper.Count, lower.Count);
        }

        // ── FilterByCategory ───────────────────────────────────────────────────────

        [Fact]
        public void FilterByCategory_MatchingId_ReturnsOnlyThatCategory()
        {
            var result = MakeProducts().FilterByCategory(3).ToList();

            Assert.Single(result);
            Assert.Equal("iPhone 15", result[0].Name);
        }

        [Fact]
        public void FilterByCategory_NullId_ReturnsAllProducts()
        {
            var result = MakeProducts().FilterByCategory(null).ToList();
            Assert.Equal(5, result.Count);
        }

        // ── FilterByStock ──────────────────────────────────────────────────────────

        [Fact]
        public void FilterByStock_True_ReturnsOnlyInStock()
        {
            var result = MakeProducts().FilterByStock(true).ToList();

            Assert.All(result, p => Assert.True(p.Quantity > 0));
            Assert.Equal(4, result.Count);
        }

        [Fact]
        public void FilterByStock_False_ReturnsOnlyOutOfStock()
        {
            var result = MakeProducts().FilterByStock(false).ToList();

            Assert.All(result, p => Assert.Equal(0, p.Quantity));
            Assert.Single(result);
            Assert.Equal("Wireless Mouse", result[0].Name);
        }

        [Fact]
        public void FilterByStock_Null_ReturnsAllProducts()
        {
            var result = MakeProducts().FilterByStock(null).ToList();
            Assert.Equal(5, result.Count);
        }

        // ── SortProducts ───────────────────────────────────────────────────────────

        [Fact]
        public void SortProducts_ByName_Ascending_SortsAlphabetically()
        {
            var result = MakeProducts().SortProducts("name", false).ToList();

            for (int i = 1; i < result.Count; i++)
                Assert.True(
                    string.Compare(result[i - 1].Name, result[i].Name,
                        StringComparison.OrdinalIgnoreCase) <= 0);
        }

        [Fact]
        public void SortProducts_ByPrice_Descending_SortsHighestFirst()
        {
            var result = MakeProducts().SortProducts("price", true).ToList();

            for (int i = 1; i < result.Count; i++)
                Assert.True(result[i - 1].Price >= result[i].Price);
        }

        [Fact]
        public void SortProducts_ByQuantity_Ascending_SortsLowestFirst()
        {
            var result = MakeProducts().SortProducts("quantity", false).ToList();

            for (int i = 1; i < result.Count; i++)
                Assert.True(result[i - 1].Quantity <= result[i].Quantity);
        }

        [Fact]
        public void SortProducts_UnknownSortKey_DefaultsToNameSort()
        {
            var result = MakeProducts().SortProducts("unknown_field", false).ToList();

            // Should sort by name ascending (default)
            for (int i = 1; i < result.Count; i++)
                Assert.True(
                    string.Compare(result[i - 1].Name, result[i].Name,
                        StringComparison.OrdinalIgnoreCase) <= 0);
        }
    }
}
