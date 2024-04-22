using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MassTransit.Producers
{
    public class SendWithMassTransitEventProducer : IEventProducer
    {

        private readonly IBus _bus;
        private readonly ILogger<PublishWithMassTransitEventProducer> _logger;
        private readonly IServiceProvider _serviceProvider;

        public SendWithMassTransitEventProducer(IBus bus, ILogger<PublishWithMassTransitEventProducer> logger, IServiceProvider serviceProvider)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task ProduceEventAsync<T>(T @event, CancellationToken cancellationToken = default) where T : ISerializableEvent
        {
            Guard.IsNotNull(@event, nameof(@event));
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                _logger.LogInformation("{0} sending event: {1}", new object[] { this.GetGenericTypeName(), @event });
                await _bus.Send(@event, cancellationToken);
            }
        }
    }
}
