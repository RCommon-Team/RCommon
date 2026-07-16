using RCommon.Persistence.Crud;

namespace Examples.Persistence.Linq2Db;

public class ProductCatalogService : IProductCatalogService
{
    private const string DataStoreName = "AppDb";

    private readonly ILinqRepository<Product> _products;

    public ProductCatalogService(ILinqRepository<Product> products)
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

    // Linq2DbRepository.FindAsync(object primaryKey) is a separate, pre-existing unimplemented gap
    // (see docs/specs/persistence/dapper-linq2db-generated-key-writeback.md) -- use the predicate
    // overload instead, which is fully implemented.
    public async Task<Product?> FindProductAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _products.FindSingleOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task RaisePriceAsync(int id, decimal newPrice, CancellationToken cancellationToken = default)
    {
        var product = await FindProductAsync(id, cancellationToken)
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
        var product = await FindProductAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Product '{id}' was not found.");

        // Product does not implement ISoftDelete, so this issues a physical DELETE.
        await _products.DeleteAsync(product, cancellationToken);
    }
}
