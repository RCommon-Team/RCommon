using Microsoft.EntityFrameworkCore;
using RCommon.Persistence.EFCore;

namespace Examples.MultiTenancy.Finbuckle;

public class AppDbContext : RCommonDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products { get; set; } = null!;
}
