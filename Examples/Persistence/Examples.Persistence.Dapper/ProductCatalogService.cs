using RCommon.Persistence.Crud;

namespace Examples.Persistence.Dapper;

public class ProductCatalogService : IProductCatalogService
{
    private const string DataStoreName = "AppDb";

    private readonly ISqlMapperRepository<Product> _products;

    public ProductCatalogService(ISqlMapperRepository<Product> products)
    {
        _products = products;
        _products.DataStoreName = DataStoreName;
    }

    public async Task<int> CreateProductAsync(string name, string sku, decimal price, int stockQuantity, CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Name = name,
            Sku = sku,
            Price = price,
            StockQuantity = stockQuantity
        };

        await _products.AddAsync(product, cancellationToken);
        return product.Id;
    }

    public async Task<Product?> FindProductAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _products.FindAsync(id, cancellationToken);
    }

    public async Task RaisePriceAsync(int id, decimal newPrice, CancellationToken cancellationToken = default)
    {
        var product = await _products.FindAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Product '{id}' was not found.");

        product.Price = newPrice;
        await _products.UpdateAsync(product, cancellationToken);
    }

    public Task<long> CountInStockAsync(CancellationToken cancellationToken = default)
    {
        return _products.GetCountAsync(p => p.StockQuantity > 0, cancellationToken);
    }

    public async Task DiscontinueAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _products.FindAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Product '{id}' was not found.");

        // Product does not implement ISoftDelete, so this issues a physical DELETE.
        await _products.DeleteAsync(product, cancellationToken);
    }
}


