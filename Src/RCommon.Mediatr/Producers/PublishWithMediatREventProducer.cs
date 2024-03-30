﻿using MediatR;
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

        public PublishWithMediatREventProducer(IMediatorService mediatorService)
        {
            _mediatorService = mediatorService;
        }

        public async Task ProduceEventAsync<T>(T @event, CancellationToken cancellationToken = default)
            where T : ISerializableEvent
        {
            await _mediatorService.Publish(new MediatRNotification<T>(@event), cancellationToken);
        }
    }
}
