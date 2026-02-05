using RCommon.Models.Events;

namespace Examples.Messaging.SubscriptionIsolation
{
    public class MassTransitOnlyEvent : ISyncEvent
    {
        public MassTransitOnlyEvent(DateTime dateTime, Guid guid)
        {
            DateTime = dateTime;
            Guid = guid;
        }

        public MassTransitOnlyEvent()
        {
        }

        public DateTime DateTime { get; }
        public Guid Guid { get; }
    }
}
