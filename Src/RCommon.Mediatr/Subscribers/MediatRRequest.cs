using MediatR;
using RCommon.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MediatR.Subscribers
{
    public class MediatRRequest<TRequest> : IMediatRRequest<TRequest>
    {

        public MediatRRequest(TRequest request)
        {
            Request = request;
        }

        public TRequest Request { get; }
    }

    public class MediatRRequest<TRequest, TResponse> : IMediatRRequest<TRequest, TResponse>
    {

        public MediatRRequest(TRequest request)
        {
            Request = request;
        }

        public TRequest Request { get; }
    }
}
