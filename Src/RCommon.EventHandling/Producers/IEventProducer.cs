using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.EventHandling.Producers
{
    public interface IEventProducer
    {
        Task ProduceEventAsync<T>(T @event) where T : ISerializableEvent;
    }
}
