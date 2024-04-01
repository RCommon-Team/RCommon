using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator.Subscribers
{
    public interface IAppRequestHandler<TRequest>
    {
        public Task HandleAsync(TRequest request, CancellationToken cancellationToken = default);
    }
}
