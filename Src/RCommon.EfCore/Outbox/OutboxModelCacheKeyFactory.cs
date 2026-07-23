using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace RCommon.Persistence.EFCore.Outbox;

/// <summary>
/// A model cache key factory that folds the outbox-owning flag into EF Core's model cache key.
/// </summary>
/// <remarks>
/// EF Core caches the built model per context type keyed by <see cref="IModelCacheKeyFactory"/>. The
/// default key is the context <see cref="System.Type"/> only, so two instances of the same context type
/// that differ solely in whether their datastore owns an outbox would otherwise share a single cached
/// model — leaking the <c>OutboxMessage</c> mapping across datastores (or hiding it). This factory makes
/// the outbox-owning flag part of the key so each configuration produces its own model.
/// </remarks>
public sealed class OutboxModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
        => (context.GetType(), OwnsOutbox(context), designTime);

    private static bool OwnsOutbox(DbContext context)
    {
        var extension = context.GetService<IDbContextOptions>().Extensions
            .OfType<OutboxDataStoreOptionsExtension>()
            .FirstOrDefault();
        return extension is not null && extension.OwnsOutbox();
    }
}
