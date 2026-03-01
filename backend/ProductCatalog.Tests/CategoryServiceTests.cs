using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProductCatalog.API.DTOs;
using ProductCatalog.API.Interfaces;
using ProductCatalog.API.Models;
using ProductCatalog.API.Services;
using Xunit;

namespace ProductCatalog.Tests
{
    /// <summary>
    /// Unit tests for CategoryService — validates CRUD business rules, tree building,
    /// circular-parent guard, and child-delete guard.
    /// </summary>
    public class CategoryServiceTests
    {
        private readonly Mock<ICategoryRepository> _repoMock = new();
        private readonly CategoryService _sut;

        public CategoryServiceTests()
        {
            _sut = new CategoryService(
                _repoMock.Object,
                NullLogger<CategoryService>.Instance);
        }

        // ── GetAllAsync ────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_ReturnsMappedDtos()
        {
            _repoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Category>
                {
                    new() { Id = 1, Name = "Electronics", Description = "Desc", ParentCategoryId = null },
                    new() { Id = 2, Name = "Computers",   Description = null,   ParentCategoryId = 1 }
                });

            var result = (await _sut.GetAllAsync()).ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal("Electronics", result[0].Name);
            Assert.Null(result[0].ParentCategoryId);
            Assert.Equal(1, result[1].ParentCategoryId);
        }

        // ── GetTreeAsync ───────────────────────────────────────────────────────────

        [Fact]
        public async Task GetTreeAsync_BuildsCorrectHierarchy()
        {
            _repoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Category>
                {
                    new() { Id = 1, Name = "Electronics", ParentCategoryId = null },
                    new() { Id = 2, Name = "Computers",   ParentCategoryId = 1 },
                    new() { Id = 3, Name = "Phones",      ParentCategoryId = 1 },
                    new() { Id = 4, Name = "Clothing",    ParentCategoryId = null }
                });

            var tree = (await _sut.GetTreeAsync()).ToList();

            // Two roots
            Assert.Equal(2, tree.Count);

            var electronics = tree.Single(n => n.Name == "Electronics");
            Assert.Equal(2, electronics.Children.Count);
            Assert.Contains(electronics.Children, c => c.Name == "Computers");
            Assert.Contains(electronics.Children, c => c.Name == "Phones");

            var clothing = tree.Single(n => n.Name == "Clothing");
            Assert.Empty(clothing.Children);
        }

        [Fact]
        public async Task GetTreeAsync_EmptyList_ReturnsEmptyRoots()
        {
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category>());

            var tree = await _sut.GetTreeAsync();

            Assert.Empty(tree);
        }

        // ── CreateAsync ────────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_ValidRoot_ReturnsSuccess()
        {
            var dto = new CreateCategoryDto("Accessories", "All accessories", null);
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Category>()))
                .ReturnsAsync((Category c) => { c.Id = 10; return c; });

            var result = await _sut.CreateAsync(dto);

            Assert.True(result.IsSuccess);
            Assert.Equal("Accessories", result.Value!.Name);
            Assert.Equal(10, result.Value.Id);
        }

        [Fact]
        public async Task CreateAsync_WithValidParent_ReturnsSuccess()
        {
            var dto = new CreateCategoryDto("Laptops", null, 1);
            _repoMock.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Category>()))
                .ReturnsAsync((Category c) => { c.Id = 11; return c; });

            var result = await _sut.CreateAsync(dto);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value!.ParentCategoryId);
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData(null)]
        public async Task CreateAsync_EmptyName_ReturnsFailure(string? name)
        {
            var dto = new CreateCategoryDto(name!, null, null);

            var result = await _sut.CreateAsync(dto);

            Assert.False(result.IsSuccess);
            Assert.Equal("Category name is required.", result.Error);
        }

        [Fact]
        public async Task CreateAsync_ParentNotFound_ReturnsFailure()
        {
            var dto = new CreateCategoryDto("Sub", null, 999);
            _repoMock.Setup(r => r.ExistsAsync(999)).ReturnsAsync(false);

            var result = await _sut.CreateAsync(dto);

            Assert.False(result.IsSuccess);
            Assert.Contains("999", result.Error);
        }

        // ── UpdateAsync ────────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_ValidDto_ReturnsUpdatedCategory()
        {
            var existing = new Category { Id = 2, Name = "Old Name", ParentCategoryId = 1 };
            var dto = new UpdateCategoryDto("New Name", "New Desc", null);

            _repoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(existing);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Category>()))
                .ReturnsAsync((Category c) => c);

            var result = await _sut.UpdateAsync(2, dto);

            Assert.True(result.IsSuccess);
            Assert.Equal("New Name", result.Value!.Name);
            Assert.Null(result.Value.ParentCategoryId);
        }

        [Fact]
        public async Task UpdateAsync_NotFound_ReturnsFailure()
        {
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Category?)null);

            var result = await _sut.UpdateAsync(99, new UpdateCategoryDto("X", null, null));

            Assert.False(result.IsSuccess);
            Assert.Equal("Category not found.", result.Error);
        }

        [Fact]
        public async Task UpdateAsync_SelfParent_ReturnsFailure()
        {
            var existing = new Category { Id = 5, Name = "Cat" };
            _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);

            // Setting ParentCategoryId = same Id as the category
            var result = await _sut.UpdateAsync(5, new UpdateCategoryDto("Cat", null, 5));

            Assert.False(result.IsSuccess);
            Assert.Contains("own parent", result.Error);
        }

        [Fact]
        public async Task UpdateAsync_ParentNotFound_ReturnsFailure()
        {
            var existing = new Category { Id = 3, Name = "Sub" };
            _repoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(existing);
            _repoMock.Setup(r => r.ExistsAsync(999)).ReturnsAsync(false);

            var result = await _sut.UpdateAsync(3, new UpdateCategoryDto("Sub", null, 999));

            Assert.False(result.IsSuccess);
            Assert.Contains("999", result.Error);
        }

        // ── DeleteAsync ────────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_LeafCategory_ReturnsSuccess()
        {
            _repoMock.Setup(r => r.ExistsAsync(5)).ReturnsAsync(true);
            _repoMock.Setup(r => r.GetChildrenAsync(5)).ReturnsAsync(new List<Category>());
            _repoMock.Setup(r => r.DeleteAsync(5)).ReturnsAsync(true);

            var result = await _sut.DeleteAsync(5);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task DeleteAsync_CategoryWithChildren_ReturnsFailure()
        {
            _repoMock.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
            _repoMock.Setup(r => r.GetChildrenAsync(1))
                .ReturnsAsync(new List<Category>
                {
                    new() { Id = 2, Name = "Child", ParentCategoryId = 1 }
                });

            var result = await _sut.DeleteAsync(1);

            Assert.False(result.IsSuccess);
            Assert.Contains("sub-categories", result.Error);
        }

        [Fact]
        public async Task DeleteAsync_NotFound_ReturnsFailure()
        {
            _repoMock.Setup(r => r.ExistsAsync(99)).ReturnsAsync(false);

            var result = await _sut.DeleteAsync(99);

            Assert.False(result.IsSuccess);
            Assert.Equal("Category not found.", result.Error);
        }
    }
}
