using Microsoft.AspNetCore.Mvc;
using ProductCatalog.API.DTOs;
using ProductCatalog.API.Interfaces;

namespace ProductCatalog.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService) =>
            _categoryService = categoryService;

        // GET /api/categories - returns flat list
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _categoryService.GetAllAsync();
            return Ok(categories);
        }

        // GET /api/categories/tree - returns hierarchical tree
        [HttpGet("tree")]
        public async Task<IActionResult> GetCategoryTree()
        {
            var tree = await _categoryService.GetTreeAsync();
            return Ok(tree);
        }

        // GET /api/categories/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            return category is null
                ? NotFound(new { error = $"Category {id} not found." })
                : Ok(category);
        }

        // POST /api/categories
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
        {
            var result = await _categoryService.CreateAsync(dto);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.Error });

            return CreatedAtAction(
                nameof(GetCategory),
                new { id = result.Value!.Id },
                result.Value);
        }

        // PUT /api/categories/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto dto)
        {
            var result = await _categoryService.UpdateAsync(id, dto);

            if (!result.IsSuccess)
            {
                if (result.Error == "Category not found.")
                    return NotFound(new { error = result.Error });

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }

        // DELETE /api/categories/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var result = await _categoryService.DeleteAsync(id);

            if (!result.IsSuccess)
            {
                if (result.Error == "Category not found.")
                    return NotFound(new { error = result.Error });

                return BadRequest(new { error = result.Error });
            }

            return NoContent();
        }
    }
}
