using Examples.MultiTenancy.Finbuckle;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.Finbuckle;
using RCommon.MultiTenancy;
using RCommon.Persistence.Crud;
using RCommon.Security.Claims;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // No strategy/store is configured -- this console app has no HTTP requests for Finbuckle's
        // usual header/route/host strategies to inspect. Instead, the tenant context is set directly
        // via IMultiTenantContextSetter below, exactly as a background job or message consumer would.
        services.AddMultiTenant<TenantInfo>();

        services.AddRCommon()
            .WithPersistence<EFCorePersistenceBuilder>(ef =>
            {
                ef.AddDbContext<AppDbContext>("AppDb", options =>
                    options.UseInMemoryDatabase("multitenancy-example"));

                ef.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "AppDb");
            })
            .WithMultiTenancy<FinbuckleMultiTenantBuilder<TenantInfo>>(mt => { });
    })
    .Build();

Console.WriteLine("Example Starting");

var tenantA = new TenantInfo { Id = "tenant-a", Identifier = "acme", Name = "Acme Corp" };
var tenantB = new TenantInfo { Id = "tenant-b", Identifier = "globex", Name = "Globex Inc" };

// AsyncLocalMultiTenantContextAccessor<TenantInfo> is registered by AddMultiTenant<TenantInfo>() as
// both IMultiTenantContextAccessor<TenantInfo> (read) and IMultiTenantContextSetter (write); setting
// it here is what app.UseMultiTenant() middleware would normally do per-request.
var contextSetter = host.Services.GetRequiredService<IMultiTenantContextSetter>();

contextSetter.MultiTenantContext = new MultiTenantContext<TenantInfo>(tenantA);
using (var scopeA = host.Services.CreateScope())
{
    var products = scopeA.ServiceProvider.GetRequiredService<IGraphRepository<Product>>();
    await products.AddAsync(new Product { Name = "Acme Widget" });
    Console.WriteLine("Created a product for tenant-a");
}

contextSetter.MultiTenantContext = new MultiTenantContext<TenantInfo>(tenantB);
using (var scopeB = host.Services.CreateScope())
{
    var products = scopeB.ServiceProvider.GetRequiredService<IGraphRepository<Product>>();
    await products.AddAsync(new Product { Name = "Globex Gadget" });
    Console.WriteLine("Created a product for tenant-b");
}

contextSetter.MultiTenantContext = new MultiTenantContext<TenantInfo>(tenantA);
using (var scopeA2 = host.Services.CreateScope())
{
    var products = scopeA2.ServiceProvider.GetRequiredService<IGraphRepository<Product>>();
    var visibleToTenantA = await products.FindAsync(p => true);
    Console.WriteLine($"Tenant-a sees {visibleToTenantA.Count} product(s): {string.Join(", ", visibleToTenantA.Select(p => p.Name))}");
}

using (var scopeAdmin = host.Services.CreateScope())
{
    var products = scopeAdmin.ServiceProvider.GetRequiredService<IGraphRepository<Product>>();

    // TenantScope.Bypass() suspends tenant filtering for this scope's lifetime, regardless of which
    // tenant context is currently set -- useful for admin/cross-tenant reporting.
    using (TenantScope.Bypass())
    {
        var allProducts = await products.FindAsync(p => true);
        Console.WriteLine($"Admin (bypassed) sees {allProducts.Count} product(s) across all tenants: {string.Join(", ", allProducts.Select(p => $"{p.Name} [{p.TenantId}]"))}");
    }
}

Console.WriteLine("Example Complete");

