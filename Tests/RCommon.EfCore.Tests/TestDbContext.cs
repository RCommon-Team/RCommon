using Microsoft.EntityFrameworkCore;
using RCommon.Persistence.EFCore;

namespace RCommon.EfCore.Tests;

/// <summary>
/// Test DbContext that inherits from RCommonDbContext for unit testing purposes.
/// </summary>
public class TestDbContext : RCommonDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<TestEntity> TestEntities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255);
        });
    }
}
