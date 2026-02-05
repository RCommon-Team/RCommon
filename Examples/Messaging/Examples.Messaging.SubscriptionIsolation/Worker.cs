using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RCommon.EventHandling.Producers;

namespace Examples.Messaging.SubscriptionIsolation
{
    public class Worker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<Worker> _logger;
        private readonly IHostApplicationLifetime _lifetime;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IHostApplicationLifetime lifetime)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _lifetime = lifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("=== Producing Events ===");
            Console.WriteLine();

            var eventProducers = _serviceProvider.GetServices<IEventProducer>();

            // Produce an event subscribed only to InMemoryEventBus
            var inMemoryEvent = new InMemoryOnlyEvent(DateTime.Now, Guid.NewGuid());
            Console.WriteLine("Publishing InMemoryOnlyEvent to all producers...");
            foreach (var producer in eventProducers)
            {
                Console.WriteLine($"  -> Producer: {producer.GetType().Name}");
                await producer.ProduceEventAsync(inMemoryEvent);
            }

            Console.WriteLine();

            // Produce an event subscribed only to MassTransit
            var massTransitEvent = new MassTransitOnlyEvent(DateTime.Now, Guid.NewGuid());
            Console.WriteLine("Publishing MassTransitOnlyEvent to all producers...");
            foreach (var producer in eventProducers)
            {
                Console.WriteLine($"  -> Producer: {producer.GetType().Name}");
                await producer.ProduceEventAsync(massTransitEvent);
            }

            Console.WriteLine();

            // Produce an event subscribed to BOTH builders - both producers should handle it
            var sharedEvent = new SharedEvent(DateTime.Now, Guid.NewGuid());
            Console.WriteLine("Publishing SharedEvent to all producers (subscribed to both)...");
            foreach (var producer in eventProducers)
            {
                Console.WriteLine($"  -> Producer: {producer.GetType().Name}");
                await producer.ProduceEventAsync(sharedEvent);
            }

            Console.WriteLine();
            Console.WriteLine("=== Example Complete ===");
            _lifetime.StopApplication();
        }
    }
}
