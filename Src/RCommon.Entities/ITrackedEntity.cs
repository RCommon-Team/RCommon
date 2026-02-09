using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Entities
{
    /// <summary>
    /// Marks an entity as capable of participating in event tracking.
    /// When <see cref="AllowEventTracking"/> is <c>true</c>, the entity's local events
    /// can be collected and emitted by an <see cref="IEntityEventTracker"/>.
    /// </summary>
    public interface ITrackedEntity
    {
        /// <summary>
        /// Gets or sets a value indicating whether this entity should have its events tracked.
        /// </summary>
        bool AllowEventTracking { get; set; }
    }
}
