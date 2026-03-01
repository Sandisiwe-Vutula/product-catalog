using ProductCatalog.API.DTOs;
using ProductCatalog.API.Interfaces;
using ProductCatalog.API.Models;
using ProductCatalog.API.Utilities;

namespace ProductCatalog.API.Services
{

    /// <summary>
    /// Service layer for product operations.
    /// handling business rules, validation, mapping, and cache coordination.
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepo;
        private readonly ICategoryRepository _categoryRepo;
        private readonly ISearchCacheService _cache;
        private readonly IProductSearchEngine<Product> _searchEngine;
        private readonly ILogger<ProductService> _logger;

        public ProductService(
            IProductRepository productRepo,
            ICategoryRepository categoryRepo,
            ISearchCacheService cache,
            IProductSearchEngine<Product> searchEngine,
            ILogger<ProductService> logger)
        {
            _productRepo = productRepo;
            _categoryRepo = categoryRepo;
            _cache = cache;
            _searchEngine = searchEngine;
            _logger = logger;
        }

        public async Task<PagedResultDto<ProductDto>> GetProductsAsync(ProductQueryDto query)
        {
            // Try cache first
            var cached = _cache.TryGet(query);
            if (cached is not null) return cached;

            var (items, totalCount) = await _productRepo.GetPagedAsync(
                query.Search, query.CategoryId, query.MinPrice, query.MaxPrice,
                query.InStock, query.SortBy, query.SortDesc, query.Page, query.PageSize);

            var dtos = items.Select(MapToDto).ToList();
            int totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

            var result = new PagedResultDto<ProductDto>(dtos, totalCount, query.Page, query.PageSize, totalPages);
            _cache.Set(query, result);
            return result;
        }

        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            var product = await _productRepo.GetByIdAsync(id);
            return product is null ? null : MapToDto(product);
        }

        public async Task<Result<ProductDto>> CreateAsync(CreateProductDto dto)
        {
            var validationError = dto switch
            {
                { Name: var n } when string.IsNullOrWhiteSpace(n) => "Product name is required.",
                { SKU: var s } when string.IsNullOrWhiteSpace(s) => "SKU is required.",
                { Price: var p } when p < 0 => "Price cannot be negative.",
                { Quantity: var q } when q < 0 => "Quantity cannot be negative.",
                { CategoryId: var c } when c <= 0 => "A valid CategoryId is required.",
                _ => null
            };

            if (validationError is not null)
                return Result<ProductDto>.Failure(validationError);

            if (!await _categoryRepo.ExistsAsync(dto.CategoryId))
                return Result<ProductDto>.Failure($"Category {dto.CategoryId} does not exist.");

            if (await _productRepo.SkuExistsAsync(dto.SKU))
                return Result<ProductDto>.Failure($"SKU '{dto.SKU}' is already in use.");

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                SKU = dto.SKU.ToUpperInvariant(),
                Price = dto.Price,
                Quantity = dto.Quantity,
                CategoryId = dto.CategoryId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _productRepo.AddAsync(product);

            _cache.Invalidate();

            _logger.LogInformation("Product created: {Id} ({Name})", created.Id, created.Name);

            return Result<ProductDto>.Success(MapToDto(created));
        }
        public async Task<Result<ProductDto>> UpdateAsync(int id, UpdateProductDto dto)
        {
            var existing = await _productRepo.GetByIdAsync(id);

            if (existing is null)
                return Result<ProductDto>.Failure("Product not found.");

            var validationError = dto switch
            {
                { Name: var n } when string.IsNullOrWhiteSpace(n) => "Product name is required.",
                { Price: var p } when p < 0 => "Price cannot be negative.",
                { Quantity: var q } when q < 0 => "Quantity cannot be negative.",
                _ => null
            };

            if (validationError is not null)
                return Result<ProductDto>.Failure(validationError);

            if (await _productRepo.SkuExistsAsync(dto.SKU, excludeId: id))
                return Result<ProductDto>.Failure($"SKU '{dto.SKU}' is already in use.");

            existing.Name = dto.Name;
            existing.Description = dto.Description;
            existing.SKU = dto.SKU.ToUpperInvariant();
            existing.Price = dto.Price;
            existing.Quantity = dto.Quantity;
            existing.CategoryId = dto.CategoryId;
            existing.UpdatedAt = DateTime.UtcNow;

            var updated = await _productRepo.UpdateAsync(existing);

            _cache.Invalidate();

            return Result<ProductDto>.Success(MapToDto(updated));
        }
        public async Task<Result<bool>> DeleteAsync(int id)
        {
            var deleted = await _productRepo.DeleteAsync(id);

            if (!deleted)
                return Result<bool>.Failure("Product not found.");

            _cache.Invalidate();

            return Result<bool>.Success(true);
        }

        /// <summary>
        /// In-memory fuzzy search using the ProductSearchEngine.
        /// </summary>
        public async Task<IEnumerable<ProductDto>> FuzzySearchAsync(string query)
        {
            var all = await _productRepo.GetAllAsync();
            var results = _searchEngine.Search(all, query);
            return results.Select(r => MapToDto(r.Item));
        }

        private static ProductDto MapToDto(Product p) => new(
            p.Id,
            p.Name,
            p.Description,
            p.SKU,
            p.Price,
            p.Quantity,
            p.CategoryId,
            p.Category?.Name,
            p.Quantity > 0,
            p.CreatedAt,
            p.UpdatedAt
        );
    }

}
