using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Entities
{
    public class LocalEventsChangedEventArgs : EventArgs
    {
        public LocalEventsChangedEventArgs(IBusinessEntity entity, ISerializableEvent eventData)
        {
            Entity=entity;
            EventData=eventData;
        }

        public IBusinessEntity Entity { get; }
        public ISerializableEvent EventData { get; }
    }
}
