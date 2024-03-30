using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MediatR.Subscribers
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
