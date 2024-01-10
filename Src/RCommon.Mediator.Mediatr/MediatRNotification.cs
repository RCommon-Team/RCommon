using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator.MediatR
{
    public class MediatRNotification<T> : INotification
    {

        public MediatRNotification(T notification)
        {
            Notification = notification;
        }

        public T Notification { get; }
    }
}
