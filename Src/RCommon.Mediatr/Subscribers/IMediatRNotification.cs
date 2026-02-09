using MediatR;
using RCommon.Mediator;

namespace RCommon.MediatR.Subscribers
{
    /// <summary>
    /// Non-generic marker interface for MediatR notifications within the RCommon framework.
    /// Extends <see cref="INotification"/> to participate in the MediatR pipeline.
    /// </summary>
    public interface IMediatRNotification: INotification
    {

    }

    /// <summary>
    /// Generic interface for MediatR notifications that wrap an underlying event payload.
    /// </summary>
    /// <typeparam name="TEvent">The type of the wrapped event payload.</typeparam>
    public interface IMediatRNotification<TEvent> : IMediatRNotification
    {
        /// <summary>
        /// Gets or sets the underlying notification event payload.
        /// </summary>
        TEvent Notification { get; set; }
    }
}
