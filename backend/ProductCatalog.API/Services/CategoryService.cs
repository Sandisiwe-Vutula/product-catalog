using ProductCatalog.API.DTOs;
using ProductCatalog.API.Interfaces;
using ProductCatalog.API.Models;
using ProductCatalog.API.Utilities;

namespace ProductCatalog.API.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repo;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(ICategoryRepository repo, ILogger<CategoryService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            var categories = await _repo.GetAllAsync();
            return categories.Select(MapToDto);
        }

        /// <summary>
        /// Builds the category tree in a single pass using a Dictionary for parent lookup.
        /// </summary>
        public async Task<IEnumerable<CategoryTreeNodeDto>> GetTreeAsync()
        {
            var all = (await _repo.GetAllAsync()).ToList();

            // Pass 1: create node shells
            var nodeMap = all.ToDictionary(
                c => c.Id,
                c => new CategoryTreeNodeDto(c.Id, c.Name, c.Description, c.ParentCategoryId,
                    new List<CategoryTreeNodeDto>())
            );

            var roots = new List<CategoryTreeNodeDto>();

            // Pass 2: wire parent–child relationships
            foreach (var node in nodeMap.Values)
            {
                if (node.ParentCategoryId.HasValue &&
                    nodeMap.TryGetValue(node.ParentCategoryId.Value, out var parent))
                    parent.Children.Add(node);
                else
                    roots.Add(node);
            }

            return roots;
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var cat = await _repo.GetByIdAsync(id);
            return cat is null ? null : MapToDto(cat);
        }

        public async Task<Result<CategoryDto>> CreateAsync(CreateCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Result<CategoryDto>.Failure("Category name is required.");

            if (dto.ParentCategoryId.HasValue &&
                !await _repo.ExistsAsync(dto.ParentCategoryId.Value))
                return Result<CategoryDto>.Failure(
                    $"Parent category {dto.ParentCategoryId} does not exist.");

            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description,
                ParentCategoryId = dto.ParentCategoryId
            };

            var created = await _repo.AddAsync(category);
            _logger.LogInformation("Category created: {Id} ({Name})", created.Id, created.Name);

            return Result<CategoryDto>.Success(MapToDto(created));
        }

        public async Task<Result<CategoryDto>> UpdateAsync(int id, UpdateCategoryDto dto)
        {
            var existing = await _repo.GetByIdAsync(id);

            if (existing is null)
                return Result<CategoryDto>.Failure("Category not found.");

            if (string.IsNullOrWhiteSpace(dto.Name))
                return Result<CategoryDto>.Failure("Category name is required.");

            // Guard: prevent a category from being its own parent
            if (dto.ParentCategoryId.HasValue && dto.ParentCategoryId.Value == id)
                return Result<CategoryDto>.Failure("A category cannot be its own parent.");

            // Guard: ensure new parent exists
            if (dto.ParentCategoryId.HasValue &&
                !await _repo.ExistsAsync(dto.ParentCategoryId.Value))
                return Result<CategoryDto>.Failure(
                    $"Parent category {dto.ParentCategoryId} does not exist.");

            existing.Name = dto.Name;
            existing.Description = dto.Description;
            existing.ParentCategoryId = dto.ParentCategoryId;

            var updated = await _repo.UpdateAsync(existing);
            _logger.LogInformation("Category updated: {Id} ({Name})", updated.Id, updated.Name);

            return Result<CategoryDto>.Success(MapToDto(updated));
        }

        public async Task<Result<bool>> DeleteAsync(int id)
        {
            if (!await _repo.ExistsAsync(id))
                return Result<bool>.Failure("Category not found.");

            var children = await _repo.GetChildrenAsync(id);
            if (children.Any())
                return Result<bool>.Failure(
                    "Cannot delete a category that has sub-categories. Remove or re-parent them first.");

            var deleted = await _repo.DeleteAsync(id);
            if (!deleted)
                return Result<bool>.Failure("Failed to delete category.");

            _logger.LogInformation("Category deleted: {Id}", id);
            return Result<bool>.Success(true);
        }

        private static CategoryDto MapToDto(Category c) =>
            new(c.Id, c.Name, c.Description, c.ParentCategoryId);
    }
}
