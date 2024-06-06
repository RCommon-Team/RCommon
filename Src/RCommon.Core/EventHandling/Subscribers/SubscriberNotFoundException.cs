using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.EventHandling.Subscribers
{
    public class SubscriberNotFoundException : GeneralException
    {
        public SubscriberNotFoundException(string message) : base(message)
        {
            
        }
    }
}
