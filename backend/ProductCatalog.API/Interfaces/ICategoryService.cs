using ProductCatalog.API.DTOs;
using ProductCatalog.API.Utilities;

namespace ProductCatalog.API.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllAsync();

        Task<IEnumerable<CategoryTreeNodeDto>> GetTreeAsync();

        Task<CategoryDto?> GetByIdAsync(int id);

        Task<Result<CategoryDto>> CreateAsync(CreateCategoryDto dto);

        Task<Result<CategoryDto>> UpdateAsync(int id, UpdateCategoryDto dto);

        Task<Result<bool>> DeleteAsync(int id);
    }
}
