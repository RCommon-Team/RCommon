using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.EventHandling.Producers
{

    public sealed class StaticEventProducerStore
    {

        private static readonly Lazy<StaticEventProducerStore> lazy =
        new Lazy<StaticEventProducerStore>(() => new StaticEventProducerStore());

        public static StaticEventProducerStore Instance { get { return lazy.Value; } }

        private StaticEventProducerStore()
        {
        }

        public static ConcurrentDictionary<Type, Type> EventProducers { get; set; }
    }
}
