using MediatR;
using RCommon.Mediator;

namespace RCommon.MediatR.Subscribers
{
    public interface IMediatRNotification: INotification
    {

    }

    public interface IMediatRNotification<TEvent> : IMediatRNotification
    {
        TEvent Notification { get; set; }
    }
}
