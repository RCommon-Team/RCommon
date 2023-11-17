using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Entities
{
    public class LocalEventsClearedEventArgs : EventArgs
    {
        public LocalEventsClearedEventArgs(IBusinessEntity entity, IReadOnlyCollection<ILocalEvent> localEvents)
        {
            Entity=entity;
            LocalEvents=localEvents;
        }

        public IBusinessEntity Entity { get; }
        public IReadOnlyCollection<ILocalEvent> LocalEvents { get; }
    }
}
