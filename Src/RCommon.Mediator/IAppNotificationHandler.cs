using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator
{
    public interface IAppNotificationHandler
    {
       
    }

    public interface IAppNotificationHandler<in T> : IAppNotificationHandler
    {
        Task HandleAsync(T notification, CancellationToken cancellationToken = default(CancellationToken));
    }
}
