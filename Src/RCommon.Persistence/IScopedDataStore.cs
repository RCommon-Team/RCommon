using System;
using System.Collections.Concurrent;

namespace RCommon.Persistence
{
    /// <summary>
    /// Represents a scoped registry of data stores, allowing multiple named data store types to be tracked
    /// within a single scope (e.g., a request or unit of work).
    /// </summary>
    public interface IScopedDataStore
    {
        /// <summary>
        /// Gets or sets the thread-safe dictionary that maps data store names to their corresponding <see cref="Type"/> entries.
        /// </summary>
        ConcurrentDictionary<string, Type> DataStores { get; set; }
    }
}