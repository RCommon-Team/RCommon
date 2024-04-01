using MediatR;

namespace RCommon.MediatR.Subscribers
{

    public interface IMediatRRequest : IRequest
    {

    }

    public interface IMediatRRequest<TRequest> : IMediatRRequest
    {
        TRequest Request { get; }
    }
}