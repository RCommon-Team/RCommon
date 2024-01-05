using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.EventHandling.Producers
{
    public class UnsupportedEventProducerException : ApplicationException
    {

        public UnsupportedEventProducerException(string message) : base(message)
        {

        }
    {
    }
}
