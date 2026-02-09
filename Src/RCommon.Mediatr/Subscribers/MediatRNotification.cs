using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MediatR.Subscribers
{
    /// <summary>
    /// Wrapper class that adapts an event of type <typeparamref name="TEvent"/> into an
    /// <see cref="IMediatRNotification{TEvent}"/> so it can be published through the MediatR notification pipeline.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event being wrapped.</typeparam>
    public class MediatRNotification<TEvent> : IMediatRNotification<TEvent>
    {

        /// <summary>
        /// Initializes a new instance of <see cref="MediatRNotification{TEvent}"/> with the specified event payload.
        /// </summary>
        /// <param name="notification">The event payload to wrap.</param>
        public MediatRNotification(TEvent notification)
        {
            Notification = notification;
        }

        /// <inheritdoc />
        public TEvent Notification { get; set; }
    }
}
