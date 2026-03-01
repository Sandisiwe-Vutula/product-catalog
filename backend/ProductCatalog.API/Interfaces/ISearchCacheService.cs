using ProductCatalog.API.DTOs;

namespace ProductCatalog.API.Interfaces
{
    public interface ISearchCacheService
    {
        PagedResultDto<ProductDto>? TryGet(ProductQueryDto query);

        void Set(ProductQueryDto query, PagedResultDto<ProductDto> result);

        void Invalidate();
    }
}