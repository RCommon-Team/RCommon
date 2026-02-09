

using System;

namespace RCommon
{
    /// <summary>
    /// Abstracts the system clock to enable testable time-dependent code and consistent time zone handling.
    /// </summary>
    /// <seealso cref="SystemTime"/>
    /// <seealso cref="ISystemTimeOptions"/>
    public interface ISystemTime
    {
        /// <summary>
        /// Gets Now.
        /// </summary>
        DateTime Now { get; }

        /// <summary>
        /// Gets kind.
        /// </summary>
        DateTimeKind Kind { get; }

        /// <summary>
        /// Is that provider supports multiple time zone.
        /// </summary>
        bool SupportsMultipleTimezone { get; }

        /// <summary>
        /// Normalizes given <see cref="DateTime"/>.
        /// </summary>
        /// <param name="dateTime">DateTime to be normalized.</param>
        /// <returns>Normalized DateTime</returns>
        DateTime Normalize(DateTime dateTime);
    }
}
