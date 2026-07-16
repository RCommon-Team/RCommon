using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RCommon
{
    /// <summary>
    /// Runs the duplicate-registration scanner once at host startup and emits a single
    /// warning (or stashes the message on the builder) if soft duplicates are detected.
    /// </summary>
    internal sealed class RCommonBootstrapDiagnosticsHostedService : IHostedService
    {
        private readonly IServiceCollection _services;
        private readonly IRCommonBuilder _builder;
        private readonly ILoggerFactory? _loggerFactory;

        public RCommonBootstrapDiagnosticsHostedService(
            IServiceCollection services,
            IRCommonBuilder builder,
            ILoggerFactory? loggerFactory = null)
        {
            _services = services;
            _builder = builder;
            _loggerFactory = loggerFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_builder is not RCommonBuilder rb || !rb.TrySetDiagnosticsRun())
            {
                return Task.CompletedTask;
            }

            var report = _services.GeneratePossibleDuplicatesServiceDescriptorsString();
            if (!string.IsNullOrWhiteSpace(report))
            {
                rb.StashDiagnostics(report);

                if (_loggerFactory is not null)
                {
                    var logger = _loggerFactory.CreateLogger<IRCommonBuilder>();
                    logger.LogWarning("RCommon bootstrap detected duplicate service registrations:\n{Report}", report);
                }
            }

            CheckForSubscriptionsWithoutProducers();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Warns when a builder type has at least one recorded event subscription but zero recorded
        /// event producers -- those subscribers are never invoked, with no error of any kind, since
        /// <see cref="RCommon.EventHandling.Producers.InMemoryTransactionalEventRouter"/> only ever
        /// dispatches through the registered <c>IEventProducer</c> collection.
        /// </summary>
        private void CheckForSubscriptionsWithoutProducers()
        {
            var subscriptionManager = _services.GetSubscriptionManager();
            if (subscriptionManager is null)
            {
                return;
            }

            foreach (var builderType in subscriptionManager.GetBuilderTypesWithSubscriptions())
            {
                if (!subscriptionManager.HasProducerForBuilder(builderType))
                {
                    _loggerFactory?.CreateLogger<IRCommonBuilder>().LogWarning(
                        "RCommon found event subscriptions registered for {BuilderType} with no matching " +
                        "IEventProducer -- subscribers on this builder will never be invoked. Call " +
                        "AddProducer<T>() for this builder, or (for InMemoryEventBusBuilder) this is " +
                        "handled automatically as of this version.",
                        builderType.Name);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
