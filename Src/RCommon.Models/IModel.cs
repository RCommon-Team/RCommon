using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Models
{
    /// <summary>
    /// Base marker interface for all models in the RCommon framework.
    /// Implementing this interface identifies a type as an RCommon model, enabling
    /// consistent type constraints across commands, queries, events, and execution results.
    /// </summary>
    public interface IModel
    {
    }
}
