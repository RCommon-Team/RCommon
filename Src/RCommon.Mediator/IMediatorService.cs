using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator
{
    public interface IMediatorService
    {
        Task Send(object notification, CancellationToken cancellationToken = default);
        Task Publish(object notification, CancellationToken cancellationToken = default);
    }
}
