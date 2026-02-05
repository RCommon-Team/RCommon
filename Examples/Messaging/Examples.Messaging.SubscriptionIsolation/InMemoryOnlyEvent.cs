using RCommon.Models.Events;

namespace Examples.Messaging.SubscriptionIsolation
{
    public class InMemoryOnlyEvent : ISyncEvent
    {
        public InMemoryOnlyEvent(DateTime dateTime, Guid guid)
        {
            DateTime = dateTime;
            Guid = guid;
        }

        public InMemoryOnlyEvent()
        {
        }

        public DateTime DateTime { get; }
        public Guid Guid { get; }
    }
}
