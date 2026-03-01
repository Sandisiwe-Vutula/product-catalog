using ProductCatalog.API.DTOs;
using ProductCatalog.API.Utilities;

namespace ProductCatalog.API.Interfaces
{
    public interface IProductService
    {
        Task<PagedResultDto<ProductDto>> GetProductsAsync(ProductQueryDto query);

        Task<ProductDto?> GetByIdAsync(int id);

        Task<Result<ProductDto>> CreateAsync(CreateProductDto dto);

        Task<Result<ProductDto>> UpdateAsync(int id, UpdateProductDto dto);

        Task<Result<bool>> DeleteAsync(int id);

        Task<IEnumerable<ProductDto>> FuzzySearchAsync(string query);
    }
}
