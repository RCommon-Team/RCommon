using System;
using Microsoft.Extensions.Options;
using RCommon.Persistence.Outbox;

namespace RCommon.Persistence.EFCore.Outbox;

/// <summary>
/// Back-compatibility shim retained so that pre-3.2.0 registrations referencing
/// <c>EFCoreOutboxStore&lt;TContext&gt;</c> continue to compile.
/// </summary>
/// <remarks>
/// <para>
/// Before 3.2.0 the outbox store was typically sub-classed or registered with a specific
/// <typeparamref name="TContext"/> type so that it could resolve the correct
/// <see cref="RCommonDbContext"/> at construction time.  The store now selects its data store
/// per call (via the <c>dataStoreName</c> argument on each
/// <see cref="IOutboxStore"/> method), so the generic parameter is no longer meaningful.
/// </para>
/// <para>
/// <strong>Migration:</strong> register <see cref="EFCoreOutboxStore"/> directly and supply the
/// data-store name to each <see cref="IOutboxStore"/> method (or use
/// <c>AddOutbox(datastoreName)</c> in your builder configuration).
/// This generic overload is retained only for source compatibility and will be removed in a
/// future major version.
/// </para>
/// </remarks>
/// <typeparam name="TContext">
/// The <see cref="RCommonDbContext"/> subtype that was previously pinned at construction.
/// Ignored at runtime — the data store is resolved per call from <see cref="IDataStoreFactory"/>.
/// </typeparam>
[Obsolete(
    "Datastore is now selected per call; register EFCoreOutboxStore and pass the datastore name " +
    "to IOutboxStore methods (or use AddOutbox with a datastore name). " +
    "The generic EFCoreOutboxStore<TContext> is retained only for source compatibility " +
    "and will be removed in a future major version.")]
public class EFCoreOutboxStore<TContext> : EFCoreOutboxStore
    where TContext : RCommonDbContext
{
    /// <inheritdoc cref="EFCoreOutboxStore(IDataStoreFactory, IOptions{OutboxOptions})"/>
    public EFCoreOutboxStore(
        IDataStoreFactory dataStoreFactory,
        IOptions<OutboxOptions> outboxOptions)
        : base(dataStoreFactory, outboxOptions)
    {
    }
}
