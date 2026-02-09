using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.EventHandling.Producers
{
    /// <summary>
    /// Exception thrown when an error occurs during event production through an <see cref="IEventProducer"/>
    /// or the <see cref="IEventRouter"/>.
    /// </summary>
    public class EventProductionException : GeneralException
    {
        /// <summary>
        /// Initializes a new instance of <see cref="EventProductionException"/> with the specified message.
        /// </summary>
        /// <param name="message">The error message.</param>
        public EventProductionException(string message) : base(message)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="EventProductionException"/> with a parameterized message.
        /// </summary>
        /// <param name="message">The message format string.</param>
        /// <param name="params">Parameters to format into the message string.</param>
        public EventProductionException(string message, object[] @params) : base(message, @params)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="EventProductionException"/> with a parameterized message and inner exception.
        /// </summary>
        /// <param name="message">The message format string.</param>
        /// <param name="exception">The inner exception that caused this error.</param>
        /// <param name="params">Parameters to format into the message string.</param>
        public EventProductionException(string message, Exception exception, object[] @params) : base(message, exception, @params)
        {

        }
    }
}
