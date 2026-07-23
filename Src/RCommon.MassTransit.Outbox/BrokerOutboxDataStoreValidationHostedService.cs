using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RCommon.Persistence;

namespace RCommon.MassTransit.Outbox;

/// <summary>
/// Startup fail-loud (MN-3): verifies every UseBrokerOutbox binding names a REGISTERED RCommon datastore
/// whose concrete DbContext type equals the TDbContext passed to UseBrokerOutbox. A mismatch means the
/// broker-outbox interceptor sits on a different DbContext than the one carrying business writes, silently
/// breaking recipe-2b atomicity.
/// </summary>
public sealed class BrokerOutboxDataStoreValidationHostedService : IHostedService
{
    private readonly IOptions<MassTransitBrokerOutboxRegistrationOptions> _brokerOutbox;
    private readonly IOptions<DataStoreFactoryOptions> _dataStores;

    public BrokerOutboxDataStoreValidationHostedService(
        IOptions<MassTransitBrokerOutboxRegistrationOptions> brokerOutbox,
        IOptions<DataStoreFactoryOptions> dataStores)
    {
        _brokerOutbox = brokerOutbox;
        _dataStores = dataStores;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var stores = _dataStores.Value.Values;
        foreach (var reg in _brokerOutbox.Value.Registrations)
        {
            var match = stores.FirstOrDefault(v =>
                string.Equals(v.Name, reg.DataStoreName, StringComparison.Ordinal));

            if (match is null)
                throw new InvalidOperationException(
                    $"UseBrokerOutbox named datastore '{reg.DataStoreName}', but no EF Core datastore with that " +
                    $"name is registered. Call ef.AddDbContext<{reg.DbContextType.Name}>(\"{reg.DataStoreName}\", ...).");

            if (match.ConcreteType != reg.DbContextType)
                throw new InvalidOperationException(
                    $"UseBrokerOutbox<{reg.DbContextType.Name}> is bound to datastore '{reg.DataStoreName}', but that " +
                    $"datastore is registered with DbContext '{match.ConcreteType.Name}'. The broker outbox must use the " +
                    $"SAME DbContext that owns the datastore's business writes, or recipe-2b atomicity is lost.");
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
