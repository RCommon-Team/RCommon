using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MediatR.Subscribers
{
    public class MediatRNotification<TEvent> : IMediatRNotification<TEvent>
    {

        public MediatRNotification(TEvent notification)
        {
            Notification = notification;
        }

        public TEvent Notification { get; set; }
    }
}
