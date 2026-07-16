namespace Examples.Persistence.Dapper;

public interface IProductCatalogService
{
    Task<int> CreateProductAsync(string name, string sku, decimal price, int stockQuantity, CancellationToken cancellationToken = default);

    Task<Product?> FindProductAsync(int id, CancellationToken cancellationToken = default);

    Task RaisePriceAsync(int id, decimal newPrice, CancellationToken cancellationToken = default);

    Task<long> CountInStockAsync(CancellationToken cancellationToken = default);

    Task DiscontinueAsync(int id, CancellationToken cancellationToken = default);
}
