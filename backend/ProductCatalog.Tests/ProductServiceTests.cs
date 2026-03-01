using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProductCatalog.API.DTOs;
using ProductCatalog.API.Interfaces;
using ProductCatalog.API.Models;
using ProductCatalog.API.Services;
using ProductCatalog.API.Utilities;
using Xunit;

namespace ProductCatalog.Tests
{
    /// <summary>
    /// Unit tests for ProductService — all dependencies mocked via Moq.
    /// </summary>
    public class ProductServiceTests
    {
        private readonly Mock<IProductRepository> _productRepoMock = new();
        private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
        private readonly Mock<ISearchCacheService> _cacheMock = new();
        private readonly Mock<IProductSearchEngine<Product>> _searchMock = new();
        private readonly ProductService _sut;

        public ProductServiceTests()
        {
            _sut = new ProductService(
                _productRepoMock.Object,
                _categoryRepoMock.Object,
                _cacheMock.Object,
                _searchMock.Object,
                NullLogger<ProductService>.Instance);
        }

        // ── GetByIdAsync ───────────────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsMappedDto()
        {
            var product = MakeProduct(1, "Laptop Pro");
            _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);

            var result = await _sut.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Laptop Pro", result.Name);
            Assert.Equal("LAP-001", result.SKU);
            Assert.True(result.InStock);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistentId_ReturnsNull()
        {
            _productRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Product?)null);

            var result = await _sut.GetByIdAsync(999);

            Assert.Null(result);
        }

        // ── CreateAsync ────────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_ValidDto_ReturnsSuccess()
        {
            var dto = new CreateProductDto("Wireless Keyboard", null, "WKB-001", 89.99m, 20, 1);

            _categoryRepoMock.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
            _productRepoMock.Setup(r => r.SkuExistsAsync("WKB-001", null)).ReturnsAsync(false);
            _productRepoMock.Setup(r => r.AddAsync(It.IsAny<Product>()))
                .ReturnsAsync((Product p) => { p.Id = 10; return p; });

            var result = await _sut.CreateAsync(dto);

            Assert.True(result.IsSuccess);
            Assert.Equal("Wireless Keyboard", result.Value!.Name);
            Assert.Equal("WKB-001", result.Value.SKU);
            _cacheMock.Verify(c => c.Invalidate(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_CategoryDoesNotExist_ReturnsFailure()
        {
            var dto = new CreateProductDto("Product", null, "SKU-X", 10m, 5, 99);
            _categoryRepoMock.Setup(r => r.ExistsAsync(99)).ReturnsAsync(false);

            var result = await _sut.CreateAsync(dto);

            Assert.False(result.IsSuccess);
            Assert.Contains("Category 99 does not exist", result.Error);
        }

        [Fact]
        public async Task CreateAsync_DuplicateSku_ReturnsFailure()
        {
            var dto = new CreateProductDto("Product", null, "DUP-001", 10m, 5, 1);
            _categoryRepoMock.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
            _productRepoMock.Setup(r => r.SkuExistsAsync("DUP-001", null)).ReturnsAsync(true);

            var result = await _sut.CreateAsync(dto);

            Assert.False(result.IsSuccess);
            Assert.Contains("already in use", result.Error);
        }

        // ── UpdateAsync ────────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_ValidDto_ReturnsUpdatedProduct()
        {
            var existing = MakeProduct(5, "Old Name");
            var dto = new UpdateProductDto("New Name", "Updated desc", "LAP-001", 999m, 10, 1);

            _productRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);
            _productRepoMock.Setup(r => r.SkuExistsAsync("LAP-001", 5)).ReturnsAsync(false);
            _productRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Product>()))
                .ReturnsAsync((Product p) => p);

            var result = await _sut.UpdateAsync(5, dto);

            Assert.True(result.IsSuccess);
            Assert.Equal("New Name", result.Value!.Name);
            _cacheMock.Verify(c => c.Invalidate(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_NotFound_ReturnsFailure()
        {
            _productRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Product?)null);
            var dto = new UpdateProductDto("Name", null, "SKU", 10m, 5, 1);

            var result = await _sut.UpdateAsync(99, dto);

            Assert.False(result.IsSuccess);
            Assert.Equal("Product not found.", result.Error);
        }

        // ── DeleteAsync ────────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_ExistingProduct_ReturnsSuccessAndInvalidatesCache()
        {
            _productRepoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

            var result = await _sut.DeleteAsync(1);

            Assert.True(result.IsSuccess);
            _cacheMock.Verify(c => c.Invalidate(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_NotFound_ReturnsFailure()
        {
            _productRepoMock.Setup(r => r.DeleteAsync(99)).ReturnsAsync(false);

            var result = await _sut.DeleteAsync(99);

            Assert.False(result.IsSuccess);
            Assert.Equal("Product not found.", result.Error);
            _cacheMock.Verify(c => c.Invalidate(), Times.Never);
        }

        // ── Helpers ────────────────────────────────────────────────────────────────

        private static Product MakeProduct(int id, string name) => new()
        {
            Id = id,
            Name = name,
            SKU = "LAP-001",
            Price = 1299m,
            Quantity = 10,
            CategoryId = 1,
            Category = new Category { Id = 1, Name = "Computers" }
        };
    }
}