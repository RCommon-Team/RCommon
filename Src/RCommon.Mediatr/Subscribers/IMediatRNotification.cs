using MediatR;

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
