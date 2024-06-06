using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.EventHandling.Subscribers
{
    public class SubscriberFactory
    {
        private readonly IServiceProvider _provider;
        private readonly IDictionary<string, Type> _types;

        public SubscriberFactory(IServiceProvider provider, IOptions<SubscriberFactoryOptions> options)
        {
            _provider = provider;
            _types = options.Value.Types;
        }

        public ISubscriber<TEvent> Resolve<TEvent>()
            where TEvent : class, ISerializableEvent
        {
            if (_types.TryGetValue(name, out var type))
            {
                return (ISubscriber<TEvent>)_provider.GetRequiredService(type);
            }

            throw new SubscriberNotFoundException($"Subscriber with name of {name} not found");
        }

        public TSubscriber Resolve<TSubscriber, TEvent>()
            where TSubscriber : class, ISubscriber<TEvent>
            where TEvent : class, ISerializableEvent
        {
            if (_types.TryGetValue(name, out var type))
            {
                return (TSubscriber)_provider.GetRequiredService(type);
            }

            throw new SubscriberNotFoundException($"Subscriber with name of {name} not found");
        }
    }
}
