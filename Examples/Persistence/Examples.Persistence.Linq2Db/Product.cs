using RCommon.Entities;

namespace Examples.Persistence.Linq2Db;

// Unlike Dapper's Dommel-based provider, LinqToDB never guesses that "Id" is a database-generated
// column -- it requires an explicit mapping (attribute or, as used here in Program.cs, fluent mapping)
// before an auto-increment key round-trips correctly. See persistence/linq2db.mdx's "Primary keys"
// section for the full explanation.
public class Product : BusinessEntity<int>
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
}
