using Examples.Bootstrapping.MultiModule.Producers;
using Microsoft.Extensions.DependencyInjection;
using RCommon;
using RCommon.EventHandling;

namespace Examples.Bootstrapping.MultiModule.Modules;

/// <summary>
/// The Notifications module re-registers <see cref="AuditProducer"/> (already registered by
/// <see cref="OrderingModule"/>). The bootstrapper detects the duplicate and keeps a single
/// descriptor, so <c>IServiceProvider.GetServices&lt;IEventProducer&gt;()</c> resolves it once.
/// </summary>
public class NotificationsModule : IServiceModule
{
    public void Configure(IServiceCollection services)
    {
        services.AddRCommon()
            .WithEventHandling<InMemoryEventBusBuilder>(eh => eh.AddProducer<AuditProducer>());
    }
}
