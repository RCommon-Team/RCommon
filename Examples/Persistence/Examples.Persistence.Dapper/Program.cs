using Dommel;
using Examples.Persistence.Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;

// Dommel's default SQL builder assumes SQL Server's identity-retrieval syntax (SCOPE_IDENTITY()).
// Registering a per-connection-type builder is the same customization point documented on
// persistence/dapper.mdx's "Dommel entity mapping" section, applied here so the auto-generated
// int Id column round-trips correctly against SQLite's last_insert_rowid() convention.
DommelMapper.AddSqlBuilder(typeof(SqliteConnection), new SqliteSqlBuilder());

var dbPath = Path.Combine(Path.GetTempPath(), "rcommon-dapper-example.db");
if (File.Exists(dbPath))
{
    File.Delete(dbPath);
}
var connectionString = $"Data Source={dbPath}";

// Dommel/Dapper only issue schema-changing DDL themselves for CRUD operations; table creation is the
// consumer's responsibility, same as against a real SQL Server/PostgreSQL database.
using (var schemaConnection = new SqliteConnection(connectionString))
{
    schemaConnection.Open();
    var createTable = schemaConnection.CreateCommand();
    // Dommel's default table name resolver pluralizes the entity type name (Product -> Products).
    // INTEGER PRIMARY KEY is SQLite's rowid alias, which auto-assigns a value on insert when Id is
    // omitted -- Dommel excludes an int-typed key from the INSERT column list by convention and
    // retrieves the generated value afterward via the registered SqliteSqlBuilder.
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
            .WithPersistence<DapperPersistenceBuilder>(dapper =>
            {
                dapper.AddDbConnection<AppDbConnection>("AppDb", options =>
                {
                    options.DbFactory = SqliteFactory.Instance;
                    options.ConnectionString = connectionString;
                });

                dapper.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "AppDb");
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
