using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator.MediatR
{
    public class MediatRNotification<TEvent> : INotification
    {

        public MediatRNotification(TEvent notification)
        {
            Notification = notification;
        }

        public TEvent Notification { get; }
    }
}
