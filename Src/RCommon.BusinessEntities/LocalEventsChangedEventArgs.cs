using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.BusinessEntities
{
    public class LocalEventsChangedEventArgs : EventArgs
    {
        public LocalEventsChangedEventArgs(IBusinessEntity entity, ILocalEvent eventData)
        {
            Entity=entity;
            EventData=eventData;
        }

        public IBusinessEntity Entity { get; }
        public ILocalEvent EventData { get; }
    }
}
