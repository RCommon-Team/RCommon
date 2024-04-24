using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using RCommon.Mediator;
using RCommon.MediatR.Subscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MediatR.Producers
{
    public class PublishWithMediatREventProducer : IEventProducer
    {
        private readonly IMediatorService _mediatorService;
        private readonly ILogger<PublishWithMediatREventProducer> _logger;
        private readonly IServiceProvider _serviceProvider;

        public PublishWithMediatREventProducer(IMediatorService mediatorService, ILogger<PublishWithMediatREventProducer> logger, IServiceProvider serviceProvider)
        {
            _mediatorService = mediatorService ?? throw new ArgumentNullException(nameof(mediatorService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : ISerializableEvent
        {
            try
            {
                Guard.IsNotNull(@event, nameof(@event));
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("{0} publishing event: {1}", new object[] { this.GetGenericTypeName(), @event.GetGenericTypeName() });
                    }
                    else
                    {
                        _logger.LogDebug("{0} publishing event: {1}", new object[] { this.GetGenericTypeName(), @event });
                    }
                    
                    
                    await _mediatorService.Publish(@event, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                throw new EventProductionException("An error occured in {0} while producing event {1}",
                    ex,
                    new object[] { this.GetGenericTypeName(), @event.GetGenericTypeName() });
            }
        }
    }
}
