using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;

namespace RCommon.Persistence.Outbox;

/// <summary>
/// Singleton registry of datastore names that own an outbox table.
/// </summary>
/// <remarks>
/// <para>
/// Ordering-safe design: <see cref="AddOutbox{TOutboxStore}"/> may be called before
/// <c>SetDefaultDataStore</c> configures <see cref="DefaultDataStoreOptions"/>. To handle this,
/// pending names are stored in <see cref="OutboxDataStoreRegistrationOptions"/> (via
/// <c>IOptions&lt;T&gt;</c>), and the default datastore name is resolved from
/// <see cref="DefaultDataStoreOptions"/> lazily — only when <see cref="Registrations"/>
/// is first read, which happens at runtime, not at DI registration time.
/// </para>
/// <para>
/// A null entry in <see cref="OutboxDataStoreRegistrationOptions.Names"/> means
/// "include the configured default datastore name" and causes this registry to fold in
/// <see cref="DefaultDataStoreOptions.DefaultDataStoreName"/> at read time.
/// </para>
/// </remarks>
public sealed class OutboxDataStoreRegistry : IOutboxDataStoreRegistry
{
    private readonly IOptions<OutboxDataStoreRegistrationOptions> _registrationOptions;
    private readonly IOptions<DefaultDataStoreOptions> _defaultOptions;

    public OutboxDataStoreRegistry(
        IOptions<OutboxDataStoreRegistrationOptions> registrationOptions,
        IOptions<DefaultDataStoreOptions> defaultOptions)
    {
        _registrationOptions = registrationOptions;
        _defaultOptions = defaultOptions;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> Registrations
    {
        get
        {
            var names = _registrationOptions.Value.Names;
            var set = new HashSet<string>(StringComparer.Ordinal);
            var includeDefault = false;

            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name))
                    includeDefault = true;
                else
                    set.Add(name);
            }

            if (includeDefault)
            {
                var defaultName = _defaultOptions.Value?.DefaultDataStoreName;
                if (!string.IsNullOrWhiteSpace(defaultName))
                    set.Add(defaultName);
            }

            return set.ToArray();
        }
    }
}
