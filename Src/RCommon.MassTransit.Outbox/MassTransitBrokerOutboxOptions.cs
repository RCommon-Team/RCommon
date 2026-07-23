using System;
using MassTransit;

namespace RCommon.MassTransit.Outbox;

/// <summary>Which relational provider MassTransit's EF Core outbox should target.</summary>
public enum BrokerOutboxProvider { None = 0, Postgres, SqlServer }

/// <summary>
/// Options for <see cref="MassTransitOutboxBuilderExtensions.UseBrokerOutbox{TDbContext}"/> (recipe 2b).
/// Captures the owning RCommon datastore name and the provider WITHOUT needing MassTransit's
/// configurator, so the wrapper can validate before calling the generic AddEntityFrameworkOutbox.
/// </summary>
public sealed class MassTransitBrokerOutboxOptions
{
    /// <summary>The RCommon datastore whose DbContext owns the broker outbox. Required (AC-14).</summary>
    public string? DataStoreName { get; private set; }

    /// <summary>The chosen relational provider. Required — RCommon cannot infer it at config time.</summary>
    public BrokerOutboxProvider Provider { get; private set; } = BrokerOutboxProvider.None;

    /// <summary>Optional customization of MassTransit's bus outbox; applied by the wrapper.</summary>
    public Action<IBusOutboxConfigurator>? BusOutboxConfigure { get; private set; }

    public MassTransitBrokerOutboxOptions OnDataStore(string dataStoreName)
    {
        if (string.IsNullOrWhiteSpace(dataStoreName))
            throw new ArgumentException("Data store name must not be null, empty, or whitespace.", nameof(dataStoreName));
        DataStoreName = dataStoreName;
        return this;
    }

    public MassTransitBrokerOutboxOptions UsePostgres() { Provider = BrokerOutboxProvider.Postgres; return this; }
    public MassTransitBrokerOutboxOptions UseSqlServer() { Provider = BrokerOutboxProvider.SqlServer; return this; }

    public MassTransitBrokerOutboxOptions UseBusOutbox(Action<IBusOutboxConfigurator>? configure = null)
    {
        BusOutboxConfigure = configure;
        return this;
    }
}
