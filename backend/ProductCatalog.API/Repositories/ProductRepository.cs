using Microsoft.EntityFrameworkCore;
using ProductCatalog.API.Data;
using ProductCatalog.API.Extensions;
using ProductCatalog.API.Interfaces;
using ProductCatalog.API.Models;

namespace ProductCatalog.API.Repositories
{
    /// <summary>
    /// EF Core product repository with paged query support
    /// </summary>
    public class ProductRepository : RepositoryBase<Product, int>, IProductRepository
    {
        public ProductRepository(AppDbContext context) : base(context) { }

        public override async Task<IEnumerable<Product>> GetAllAsync() =>
            await _context.Products.Include(p => p.Category).ToListAsync();

        public override async Task<Product?> GetByIdAsync(int id) =>
            await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);

        public async Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
            string? search,
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            bool? inStock,
            string sortBy,
            bool sortDesc,
            int page,
            int pageSize)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            query = query
                .FilterBySearch(search)
                .FilterByCategory(categoryId)
                .FilterByPriceRange(minPrice, maxPrice)
                .FilterByStock(inStock)
                .SortProducts(sortBy, sortDesc);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<bool> SkuExistsAsync(string sku, int? excludeId = null)
        {
            var query = _context.Products.Where(p => p.SKU == sku);
            if (excludeId.HasValue)
                query = query.Where(p => p.Id != excludeId.Value);
            return await query.AnyAsync();
        }
    }

}
