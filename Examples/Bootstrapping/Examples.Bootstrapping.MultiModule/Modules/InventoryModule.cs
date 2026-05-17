using Examples.Bootstrapping.MultiModule.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RCommon;
using RCommon.Caching;
using RCommon.MemoryCache;

namespace Examples.Bootstrapping.MultiModule.Modules;

/// <summary>
/// The Inventory module configures its own DbContext under a distinct data-store name,
/// re-declares the same guid generator (idempotent no-op), and adds in-memory caching.
/// </summary>
public class InventoryModule : IServiceModule
{
    public void Configure(IServiceCollection services)
    {
        services.AddRCommon()
            .WithSimpleGuidGenerator() // Same impl as Ordering -> idempotent no-op.
            .WithPersistence<EFCorePerisistenceBuilder>(ef =>
                ef.AddDbContext<InventoryDbContext>(
                    "Inventory",
                    o => o.UseInMemoryDatabase("inventory")))
            .WithMemoryCaching<InMemoryCachingBuilder>();
    }
}
