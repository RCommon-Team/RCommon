using MediatR;

namespace RCommon.MediatR.Subscribers
{

    public interface IMediatRRequest : IRequest
    {

    }

    public interface IMediatRRequest<TResponse> : IRequest<TResponse>, IMediatRRequest
    {

    }

    public interface IMediatRRequest<TRequest, TResponse> : IMediatRRequest<TResponse>
    {
        TRequest Request { get; }
    }
}