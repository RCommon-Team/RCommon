using System;
using System.Collections.Generic;

namespace RCommon.MassTransit.Outbox;

/// <summary>Accumulates (datastore name → DbContext type) bindings declared via UseBrokerOutbox,
/// for the startup co-location validation (Task 2).</summary>
public sealed class MassTransitBrokerOutboxRegistrationOptions
{
    public List<BrokerOutboxRegistration> Registrations { get; } = new();
    public void Register(string dataStoreName, Type dbContextType)
        => Registrations.Add(new BrokerOutboxRegistration(dataStoreName, dbContextType));
}

public sealed record BrokerOutboxRegistration(string DataStoreName, Type DbContextType);
