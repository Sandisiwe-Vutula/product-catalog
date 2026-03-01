using ProductCatalog.API.Models;

namespace ProductCatalog.API.Interfaces
{
    public interface IProductRepository : IRepository<Product, int>
    {
        Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
            string? search,
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            bool? inStock,
            string sortBy,
            bool sortDesc,
            int page,
            int pageSize);

        Task<bool> SkuExistsAsync(string sku, int? excludeId = null);
    }
}
