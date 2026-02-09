using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Models.Queries
{
    /// <summary>
    /// Marker interface representing a query in the CQRS pattern.
    /// Queries encapsulate the intent to read data without modifying system state.
    /// </summary>
    public interface IQuery
    {
    }

    /// <summary>
    /// Generic query interface that specifies the expected result type.
    /// Use this interface when a query handler should return a typed result.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the query handler.</typeparam>
    /// <seealso cref="IQuery"/>
    public interface IQuery<TResult> : IQuery
    {
    }
}
