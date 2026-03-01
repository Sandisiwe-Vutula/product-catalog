using ProductCatalog.API.Models;

namespace ProductCatalog.API.Interfaces
{
    public interface ICategoryRepository : IRepository<Category, int>
    {
        Task<IEnumerable<Category>> GetRootCategoriesAsync();
        Task<IEnumerable<Category>> GetChildrenAsync(int parentId);
    }
}
