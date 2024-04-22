using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wolverine;

namespace RCommon.Wolverine.Producers
{
    public class PublishWithWolverineEventProducer : IEventProducer
    {
        private readonly IMessageBus _messageBus;
        private readonly ILogger<PublishWithWolverineEventProducer> _logger;
        private readonly IServiceProvider _serviceProvider;

        public PublishWithWolverineEventProducer(IMessageBus messageBus, ILogger<PublishWithWolverineEventProducer> logger, IServiceProvider serviceProvider)
        {
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        } 

        public async Task ProduceEventAsync<T>(T @event, CancellationToken cancellationToken = default) where T : ISerializableEvent
        {
            Guard.IsNotNull(@event, nameof(@event));
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                _logger.LogInformation("{0} publishing event: {1}", new object[] { this.GetGenericTypeName(), @event.GetGenericTypeName() });
                await _messageBus.PublishAsync<T>(@event);
            }
        }
    }
}
