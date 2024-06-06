using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Entities
{
    public class TransactionalEventsClearedEventArgs : EventArgs
    {
        public TransactionalEventsClearedEventArgs(IBusinessEntity entity)
        {
            Entity = entity;
        }

        public IBusinessEntity Entity { get; }
    }
}
