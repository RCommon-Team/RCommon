using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.EventHandling.Subscribers
{
    public class SubscriberFactoryOptions
    {
        public IDictionary<string, Type> Types { get; } = new Dictionary<string, Type>();

        public void Register<TSubscriber, TEvent>() 
            where TSubscriber : class, ISubscriber<TEvent>
        {
            Types.Add(typeof(TSubscriber).AssemblyQualifiedName, typeof(TSubscriber));
        }
    }
}
