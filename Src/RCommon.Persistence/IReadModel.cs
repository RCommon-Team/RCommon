using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    /// <summary>
    /// Marker interface for read model entities used in CQRS-style query projections.
    /// </summary>
    /// <remarks>
    /// Implementing this interface signals that the entity is intended for read-only query scenarios
    /// and should not be used for write (command) operations.
    /// </remarks>
    public interface IReadModel
    {
    }
}
