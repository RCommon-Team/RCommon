using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator.Subscribers
{
    /// <summary>
    /// Marker interface for notification messages that are broadcast to multiple handlers.
    /// </summary>
    /// <remarks>
    /// Implement this interface on DTOs that represent events or notifications which should
    /// be dispatched to all registered subscribers via <see cref="IMediatorService.Publish{TNotification}"/>.
    /// Unlike <see cref="IAppRequest"/>, notifications can have zero or more handlers.
    /// </remarks>
    public interface IAppNotification
    {
    }
}
