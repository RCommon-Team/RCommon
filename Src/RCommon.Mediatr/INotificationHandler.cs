using MediatR;

namespace RCommon.Mediator.MediatR
{
    public interface INotificationHandler<TNotification, TEvent> : INotificationHandler<TNotification>
        where TNotification : MediatRNotification<TEvent>
    {
    }
}