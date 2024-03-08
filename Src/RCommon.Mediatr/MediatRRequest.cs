using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator.MediatR
{
    public class MediatRRequest<TRequest, TResponse> : IRequest<TResponse>
    {

        public MediatRRequest(TRequest request)
        {
            Request = request;
        }

        public TRequest Request { get; }
    }
}
