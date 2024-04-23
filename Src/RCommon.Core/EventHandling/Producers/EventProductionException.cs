using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.EventHandling.Producers
{
    public class EventProductionException : GeneralException
    {
        public EventProductionException(string message) : base(message)
        {
                
        }

        public EventProductionException(string message, object[] @params) : base(message, @params)
        {

        }

        public EventProductionException(string message, Exception exception, object[] @params) : base(message, exception, @params)
        {

        }
    }
}
