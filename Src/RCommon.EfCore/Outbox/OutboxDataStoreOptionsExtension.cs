using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Persistence.Outbox;

namespace RCommon.Persistence.EFCore.Outbox;

/// <summary>
/// A <see cref="DbContextOptions"/> extension that tags a <see cref="RCommonDbContext"/> with the
/// datastore name it was registered under and the <see cref="IOutboxDataStoreRegistry"/> that knows
/// which datastores own an outbox. <see cref="RCommonDbContext.OnModelCreating"/> reads this extension
/// to decide whether to auto-map the <c>OutboxMessage</c> entity.
/// </summary>
/// <remarks>
/// This is the seam that lets the base context learn its own registered datastore name and consult the
/// registry during model building, without requiring scoped-service injection into
/// <see cref="RCommonDbContext.OnModelCreating"/>.
/// </remarks>
public sealed class OutboxDataStoreOptionsExtension : IDbContextOptionsExtension
{
    /// <summary>
    /// The datastore name this context was registered under.
    /// </summary>
    public string DataStoreName { get; }

    /// <summary>
    /// The registry of datastore names that own an outbox table.
    /// </summary>
    public IOutboxDataStoreRegistry Registry { get; }

    public OutboxDataStoreOptionsExtension(string dataStoreName, IOutboxDataStoreRegistry registry)
    {
        DataStoreName = dataStoreName;
        Registry = registry;
    }

    /// <summary>
    /// Returns <c>true</c> when the registered datastore name is present in the outbox registry, meaning
    /// the <c>OutboxMessage</c> entity should be auto-mapped for this context.
    /// </summary>
    public bool OwnsOutbox()
    {
        if (Registry is null || string.IsNullOrWhiteSpace(DataStoreName))
        {
            return false;
        }

        foreach (var name in Registry.Registrations)
        {
            if (string.Equals(name, DataStoreName, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    // Register a model cache key factory so the outbox-owning flag participates in EF Core's model
    // cache key. Without this, two instances of the same context type that differ only in outbox
    // ownership would share one cached model, leaking (or hiding) the OutboxMessage mapping.
    public void ApplyServices(IServiceCollection services)
    {
        services.AddSingleton<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, OutboxModelCacheKeyFactory>();
    }

    public void Validate(IDbContextOptions options) { }

    private DbContextOptionsExtensionInfo? _info;

    public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

    private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
    {
        public ExtensionInfo(IDbContextOptionsExtension extension) : base(extension) { }

        private new OutboxDataStoreOptionsExtension Extension => (OutboxDataStoreOptionsExtension)base.Extension;

        // Does not change the EF service provider — safe to treat as non-database extension.
        public override bool IsDatabaseProvider => false;

        public override string LogFragment => $"OutboxDataStore={Extension.DataStoreName} ";

        public override int GetServiceProviderHashCode() => 0;

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => true;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            debugInfo["RCommon:OutboxDataStore"] = Extension.DataStoreName ?? string.Empty;
        }
    }
}
