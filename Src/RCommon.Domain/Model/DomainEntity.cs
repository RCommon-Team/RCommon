using RCommon.Domain.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Domain.Model
{
    public class DomainEntity
    {
        public DomainEntity()
        {
            this.DomainEvents = new List<DomainEvent>();
        }

        public ICollection<DomainEvent> DomainEvents { get;}
    }
}
