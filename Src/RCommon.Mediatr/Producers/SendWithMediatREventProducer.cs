using RCommon.EventHandling.Producers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using RCommon.MediatR.Subscribers;
using RCommon.Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Models.Events;

namespace RCommon.MediatR.Producers
{
    public class SendWithMediatREventProducer : IEventProducer
    {
        private readonly IMediatorService _mediatorService;
        private readonly ILogger<SendWithMediatREventProducer> _logger;
        private readonly IServiceProvider _serviceProvider;

        public SendWithMediatREventProducer(IMediatorService mediatorService, ILogger<SendWithMediatREventProducer> logger, IServiceProvider serviceProvider)
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
                        _logger.LogInformation("{0} sending event: {1}", new object[] { this.GetGenericTypeName(), @event.GetGenericTypeName() });
                    }
                    else
                    {
                        _logger.LogDebug("{0} sending event: {1}", new object[] { this.GetGenericTypeName(), @event });
                    }
                    await _mediatorService.Send(@event, cancellationToken);
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
