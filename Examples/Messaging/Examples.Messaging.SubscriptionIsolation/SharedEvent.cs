using RCommon.Models.Events;

namespace Examples.Messaging.SubscriptionIsolation
{
    public class SharedEvent : ISyncEvent
    {
        public SharedEvent(DateTime dateTime, Guid guid)
        {
            DateTime = dateTime;
            Guid = guid;
        }

        public SharedEvent()
        {
        }

        public DateTime DateTime { get; }
        public Guid Guid { get; }
    }
}
