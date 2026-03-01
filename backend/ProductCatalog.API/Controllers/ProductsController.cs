using Microsoft.AspNetCore.Mvc;
using ProductCatalog.API.DTOs;
using ProductCatalog.API.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProductCatalog.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService) =>
            _productService = productService;

        // GET /api/products - Get products with advanced filtering, sorting, and pagination
        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            // Manual model binding, reading directly from request rather than [FromQuery]
            var qs = HttpContext.Request.Query;

            var query = new ProductQueryDto(
                Search: qs.TryGetValue("search", out var s) ? s.ToString() : null,
                CategoryId: qs.TryGetValue("categoryId", out var c) && int.TryParse(c, out var cid) ? cid : null,
                MinPrice: qs.TryGetValue("minPrice", out var min) && decimal.TryParse(min, out var mnp) ? mnp : null,
                MaxPrice: qs.TryGetValue("maxPrice", out var max) && decimal.TryParse(max, out var mxp) ? mxp : null,
                InStock: qs.TryGetValue("inStock", out var is_) && bool.TryParse(is_, out var ist) ? ist : null,
                SortBy: qs.TryGetValue("sortBy", out var sb) ? sb.ToString() : "name",
                SortDesc: qs.TryGetValue("sortDesc", out var sd) && bool.TryParse(sd, out var sdb) && sdb,
                Page: qs.TryGetValue("page", out var pg) && int.TryParse(pg, out var pgi) ? Math.Max(1, pgi) : 1,
                PageSize: qs.TryGetValue("pageSize", out var ps) && int.TryParse(ps, out var psi) ? Math.Clamp(psi, 1, 100) : 10
            );

            var result = await _productService.GetProductsAsync(query);
            return Ok(result);
        }

        // GET /api/products/{id} - Get product by ID with null handling and custom error response
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _productService.GetByIdAsync(id);

            return product is null
                ? NotFound(new { error = $"Product {id} not found." })
                : Ok(product);
        }

        // GET /api/products/search?q= - Fuzzy search endpoint with custom JSON serialization
        [HttpGet("search")]
        public async Task<IActionResult> FuzzySearch([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { error = "Search query 'q' is required." });

            var products = await _productService.FuzzySearchAsync(q);

            // Custom JSON serialization, building the response document manually
            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            };

            // Wrap in a custom envelope that the auto-serializer wouldn't produce
            var envelope = new
            {
                query = q,
                timestamp = DateTime.UtcNow,
                resultCount = products.Count(),
                results = products
            };

            var json = JsonSerializer.Serialize(envelope, serializerOptions);
            return Content(json, "application/json");
        }

        // POST /api/products - Create a new product
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
        {
            var result = await _productService.CreateAsync(dto);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.Error });

            return CreatedAtAction(
                nameof(GetProduct),
                new { id = result.Value!.Id },
                result.Value);
        }

        // PUT /api/products/{id} - Update an existing product
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto dto)
        {
            var result = await _productService.UpdateAsync(id, dto);

            if (!result.IsSuccess)
            {
                if (result.Error == "Product not found.")
                    return NotFound(new { error = result.Error });

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }

        // DELETE /api/products/{id} - Delete a product
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var result = await _productService.DeleteAsync(id);

            if (!result.IsSuccess)
                return NotFound(new { error = result.Error });

            return NoContent();
        }
    }
}