using ProductCatalog.API.Interfaces;
using ProductCatalog.API.Models;

namespace ProductCatalog.API.Repositories
{
    /// <summary>
    /// Pure in-memory category repository using Dictionary and List — no Entity Framework.
    /// </summary>
    public class InMemoryCategoryRepository : ICategoryRepository
    {
        private readonly Dictionary<int, Category> _store;
        private readonly object _lock = new();
        private int _nextId;

        public InMemoryCategoryRepository()
        {
            // Seeding with same categories as EF seed data so both repos stay consistent
            _store = new Dictionary<int, Category>
            {
                [1] = new Category { Id = 1, Name = "Electronics", Description = "Electronic devices and accessories", ParentCategoryId = null },
                [2] = new Category { Id = 2, Name = "Computers", Description = "Desktops, laptops, and peripherals", ParentCategoryId = 1 },
                [3] = new Category { Id = 3, Name = "Phones", Description = "Smartphones and accessories", ParentCategoryId = 1 },
                [4] = new Category { Id = 4, Name = "Clothing", Description = "Apparel and fashion", ParentCategoryId = null },
                [5] = new Category { Id = 5, Name = "Mens", Description = "Men's clothing", ParentCategoryId = 4 },
                [6] = new Category { Id = 6, Name = "Womens", Description = "Women's clothing", ParentCategoryId = 4 },
            };
            _nextId = _store.Keys.Max() + 1;
        }

        public Task<IEnumerable<Category>> GetAllAsync() =>
            Task.FromResult<IEnumerable<Category>>(_store.Values.ToList());

        public Task<Category?> GetByIdAsync(int id) =>
            Task.FromResult(_store.TryGetValue(id, out var cat) ? cat : null);

        public Task<Category> AddAsync(Category entity)
        {
            lock (_lock)
            {
                entity.Id = _nextId++;
                _store[entity.Id] = entity;
            }
            return Task.FromResult(entity);
        }

        public Task<Category> UpdateAsync(Category entity)
        {
            lock (_lock)
            {
                if (!_store.ContainsKey(entity.Id))
                    throw new KeyNotFoundException($"Category {entity.Id} not found.");
                _store[entity.Id] = entity;
            }
            return Task.FromResult(entity);
        }

        public Task<bool> DeleteAsync(int id)
        {
            lock (_lock)
            {
                bool hasChildren = _store.Values.Any(c => c.ParentCategoryId == id);
                if (hasChildren) return Task.FromResult(false);
                return Task.FromResult(_store.Remove(id));
            }
        }

        public Task<bool> ExistsAsync(int id) =>
            Task.FromResult(_store.ContainsKey(id));

        public Task<IEnumerable<Category>> GetRootCategoriesAsync() =>
            Task.FromResult<IEnumerable<Category>>(
                _store.Values.Where(c => c.ParentCategoryId is null).ToList());

        public Task<IEnumerable<Category>> GetChildrenAsync(int parentId) =>
            Task.FromResult<IEnumerable<Category>>(
                _store.Values.Where(c => c.ParentCategoryId == parentId).ToList());
    }

}
