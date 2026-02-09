using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.EventHandling.Producers
{
    /// <summary>
    /// Exception thrown when an attempt is made to use an <see cref="IEventProducer"/> that is not
    /// supported or not properly configured for the current event handling pipeline.
    /// </summary>
    public class UnsupportedEventProducerException : ApplicationException
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UnsupportedEventProducerException"/> with the specified message.
        /// </summary>
        /// <param name="message">The error message describing the unsupported producer.</param>
        public UnsupportedEventProducerException(string message) : base(message)
        {

        }

    }
}
