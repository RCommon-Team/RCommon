using System.Collections.Generic;

namespace RCommon.Persistence.Outbox;

/// <summary>
/// Tracks which datastore names own an outbox. This is the single source of truth used by
/// the outbox poller, DbContext auto-mapping, and startup validation.
/// </summary>
public interface IOutboxDataStoreRegistry
{
    /// <summary>
    /// Returns the set of datastore names that have been registered as outbox owners.
    /// Names are deduplicated. When <see cref="AddOutbox{TOutboxStore}"/> was called without
    /// an explicit name, the default datastore name (resolved lazily from
    /// <see cref="DefaultDataStoreOptions"/>) is included here.
    /// </summary>
    IReadOnlyCollection<string> Registrations { get; }
}
