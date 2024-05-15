using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Transactions
{
    public record UnitOfWorkRolledBackEvent : ISyncEvent
    {
        public UnitOfWorkRolledBackEvent(Guid transactionId)
        {
            TransactionId = transactionId;
        }

        public Guid TransactionId { get; }
    }
}
