using ProductCatalog.API.Utilities;

namespace ProductCatalog.API.Interfaces
{
    public interface IProductSearchEngine<T>
    {
        IProductSearchEngine<T> AddField(
            string fieldName,
            Func<T, string?> accessor,
            double weight = 1.0);

        IEnumerable<SearchResult<T>> Search(
            IEnumerable<T> candidates,
            string query);
    }
}
