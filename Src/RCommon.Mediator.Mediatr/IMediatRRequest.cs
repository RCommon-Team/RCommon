using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator.MediatR
{
    public interface IMediatRRequest : IRequest, IRequestor
    {
    }

    public interface IMediatRRequest<out TResponse> : IRequest<TResponse>, IRequestor
    {
    }
}
