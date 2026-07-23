using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using RCommon.Persistence.Outbox;

namespace RCommon.Persistence.EFCore.Outbox;

public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Tags this context's options with the datastore <paramref name="dataStoreName"/> it is registered
    /// under and the <paramref name="registry"/> that knows which datastores own an outbox. When the name
    /// is present in the registry, <see cref="RCommonDbContext"/> auto-maps the <c>OutboxMessage</c> entity
    /// during model building.
    /// </summary>
    public static DbContextOptionsBuilder UseOutboxDataStore(
        this DbContextOptionsBuilder optionsBuilder,
        string dataStoreName,
        IOutboxDataStoreRegistry registry)
    {
        var extension = new OutboxDataStoreOptionsExtension(dataStoreName, registry);
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
        return optionsBuilder;
    }
}
