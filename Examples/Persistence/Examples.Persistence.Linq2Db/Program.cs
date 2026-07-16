using Examples.Persistence.Linq2Db;
using LinqToDB;
using LinqToDB.Mapping;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.Persistence.Linq2Db;

// Unlike Dommel (used by the Dapper provider), LinqToDB never infers an identity column from the
// "Id" naming convention -- without this mapping, AddAsync would try to insert an explicit 0 for
// every new Product, and the second insert would fail with a primary-key violation. See
// persistence/linq2db.mdx's "Primary keys" section.
new FluentMappingBuilder(MappingSchema.Default)
    .Entity<Product>().HasTableName("Products").Property(p => p.Id).IsPrimaryKey().IsIdentity()
    .Build();

var dbPath = Path.Combine(Path.GetTempPath(), "rcommon-linq2db-example.db");
if (File.Exists(dbPath))
{
    File.Delete(dbPath);
}
var connectionString = $"Data Source={dbPath}";

using (var schemaConnection = new SqliteConnection(connectionString))
{
    schemaConnection.Open();
    var createTable = schemaConnection.CreateCommand();
    createTable.CommandText = """
        CREATE TABLE Products (
            Id INTEGER PRIMARY KEY,
            Name TEXT NOT NULL,
            Sku TEXT NOT NULL,
            Price REAL NOT NULL,
            StockQuantity INTEGER NOT NULL
        );
        """;
    createTable.ExecuteNonQuery();
}

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddRCommon()
            .WithPersistence<Linq2DbPersistenceBuilder>(linq2db =>
            {
                linq2db.AddDataConnection<AppDataConnection>("AppDb",
                    (sp, options) => options.UseSQLite(connectionString));

                linq2db.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "AppDb");
            });

        services.AddTransient<IProductCatalogService, ProductCatalogService>();
    })
    .Build();

Console.WriteLine("Example Starting");
var catalog = host.Services.GetRequiredService<IProductCatalogService>();

var productId = await catalog.CreateProductAsync("Trail Running Shoe", "TRS-001", 89.99m, stockQuantity: 25);
Console.WriteLine($"Created product {productId}");

var product = await catalog.FindProductAsync(productId);
Console.WriteLine($"Loaded product: {product?.Name}, price {product?.Price:C}");

await catalog.RaisePriceAsync(productId, 94.99m);
var repriced = await catalog.FindProductAsync(productId);
Console.WriteLine($"New price: {repriced?.Price:C}");

var inStockCount = await catalog.CountInStockAsync();
Console.WriteLine($"Products in stock: {inStockCount}");

await catalog.DiscontinueAsync(productId);
var afterDelete = await catalog.FindProductAsync(productId);
Console.WriteLine($"Product after discontinue (expect none found): {(afterDelete is null ? "none found" : afterDelete.Name)}");

Console.WriteLine("Example Complete");
