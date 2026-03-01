namespace ProductCatalog.API.DTOs
{

    // ------------------------------- Product DTOs --------------------------------------------
    public record ProductDto(
        int Id,
        string Name,
        string? Description,
        string SKU,
        decimal Price,
        int Quantity,
        int CategoryId,
        string? CategoryName,
        bool InStock,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );

    public record CreateProductDto(
        string Name,
        string? Description,
        string SKU,
        decimal Price,
        int Quantity,
        int CategoryId
    );

    public record UpdateProductDto(
        string Name,
        string? Description,
        string SKU,
        decimal Price,
        int Quantity,
        int CategoryId
    );

    // --------------------------------------Category DTOs ----------------------------------
    public record CategoryDto(
        int Id,
        string Name,
        string? Description,
        int? ParentCategoryId
    );

    public record CategoryTreeNodeDto(
        int Id,
        string Name,
        string? Description,
        int? ParentCategoryId,
        List<CategoryTreeNodeDto> Children
    );

    public record CreateCategoryDto(
        string Name,
        string? Description,
        int? ParentCategoryId
    );

    public record UpdateCategoryDto(
    string Name,
    string? Description,
    int? ParentCategoryId
);

    // ----------------------------- Shared Pagination & Search -------------------------------

    public record PagedResultDto<T>(
        IEnumerable<T> Items,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages
    );

    public record ProductQueryDto(
        string? Search,
        int? CategoryId,
        decimal? MinPrice,
        decimal? MaxPrice,
        bool? InStock,
        string SortBy = "name",
        bool SortDesc = false,
        int Page = 1,
        int PageSize = 10
    );
}
