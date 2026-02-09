

using System;

namespace RCommon
{
    /// <summary>
    /// Defines configuration options for the <see cref="ISystemTime"/> abstraction.
    /// </summary>
    /// <seealso cref="SystemTimeOptions"/>
    public interface ISystemTimeOptions
    {
        /// <summary>
        /// Gets or sets the <see cref="DateTimeKind"/> that controls whether the system time
        /// operates in UTC, Local, or Unspecified mode.
        /// </summary>
        DateTimeKind Kind { get; set; }
    }
}
