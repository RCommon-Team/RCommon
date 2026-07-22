using System.Collections.Generic;

namespace RCommon.Persistence.Outbox;

/// <summary>
/// Accumulates the datastore names (or null for "use the default") supplied via
/// <see cref="AddOutbox{TOutboxStore}"/> calls. Each call appends to <see cref="Names"/> via
/// <c>services.Configure&lt;OutboxDataStoreRegistrationOptions&gt;(o => o.Names.Add(name))</c>,
/// which is ordering-safe because the options system defers evaluation until the provider
/// is first built.
/// </summary>
public class OutboxDataStoreRegistrationOptions
{
    /// <summary>
    /// Pending datastore name registrations. A null entry means "include the configured
    /// default datastore name" (resolved lazily from <see cref="DefaultDataStoreOptions"/>).
    /// </summary>
    public List<string?> Names { get; set; } = new();
}
