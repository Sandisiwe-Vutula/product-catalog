using ProductCatalog.API.Models;

namespace ProductCatalog.API.Extensions
{

    /// <summary>
    /// Custom LINQ extension methods for IQueryable<Product>.
    /// </summary>
    public static class ProductQueryExtensions
    {
        /// <summary>
        /// Filters products by name or description containing the search term.
        /// Null/empty search returns the original queryable unchanged.
        /// </summary>
        public static IQueryable<Product> FilterBySearch(this IQueryable<Product> query, string? search)
        {
            if (string.IsNullOrWhiteSpace(search)) return query;

            var term = search.Trim().ToLower();
            return query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                (p.Description != null && p.Description.ToLower().Contains(term)) ||
                p.SKU.ToLower().Contains(term));
        }

        public static IQueryable<Product> FilterByCategory(this IQueryable<Product> query, int? categoryId) =>
            categoryId.HasValue ? query.Where(p => p.CategoryId == categoryId.Value) : query;

        public static IQueryable<Product> FilterByPriceRange(
            this IQueryable<Product> query,
            decimal? minPrice,
            decimal? maxPrice)
        {
            if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);
            return query;
        }

        public static IQueryable<Product> FilterByStock(this IQueryable<Product> query, bool? inStock) =>
            inStock switch
            {
                true => query.Where(p => p.Quantity > 0),
                false => query.Where(p => p.Quantity == 0),
                null => query
            };

        /// <summary>
        /// Applies dynamic sorting. Uses a switch expression (pattern matching) to select
        /// the sort key, then applies asc/desc based on the sortDesc flag.
        /// </summary>
        public static IQueryable<Product> SortProducts(
            this IQueryable<Product> query,
            string sortBy,
            bool sortDesc)
        {
            IOrderedQueryable<Product> ordered = sortBy.ToLower() switch
            {
                "price" => sortDesc ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                "quantity" => sortDesc ? query.OrderByDescending(p => p.Quantity) : query.OrderBy(p => p.Quantity),
                "createdat" => sortDesc ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
                "sku" => sortDesc ? query.OrderByDescending(p => p.SKU) : query.OrderBy(p => p.SKU),
                _ => sortDesc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)
            };
            return ordered;
        }
    }

}
