

using System;
using Microsoft.Extensions.Options;

namespace RCommon
{
    /// <summary>
    /// Default implementation of <see cref="ISystemTime"/> that provides the current date/time
    /// based on the configured <see cref="DateTimeKind"/> and normalizes <see cref="DateTime"/> values accordingly.
    /// </summary>
    public class SystemTime : ISystemTime
    {
        /// <summary>
        /// Gets the configuration options for this system time instance.
        /// </summary>
        protected SystemTimeOptions Options { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="SystemTime"/> with the specified options.
        /// </summary>
        /// <param name="options">The options controlling the <see cref="DateTimeKind"/> behavior.</param>
        public SystemTime(IOptions<SystemTimeOptions> options)
        {
            Options = options.Value;
        }

        /// <inheritdoc />
        public virtual DateTime Now => Options.Kind == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now;

        /// <inheritdoc />
        public virtual DateTimeKind Kind => Options.Kind;

        /// <inheritdoc />
        public virtual bool SupportsMultipleTimezone => Options.Kind == DateTimeKind.Utc;

        /// <inheritdoc />
        /// <remarks>
        /// Conversion rules: if <see cref="Kind"/> is <see cref="DateTimeKind.Unspecified"/> or matches the
        /// input's Kind, the value is returned unchanged. Otherwise UTC-to-Local or Local-to-UTC conversion
        /// is performed. If the input Kind is <see cref="DateTimeKind.Unspecified"/>, the Kind is simply
        /// re-specified without conversion.
        /// </remarks>
        public virtual DateTime Normalize(DateTime dateTime)
        {
            // No conversion needed when Kind is unspecified or matches the input
            if (Kind == DateTimeKind.Unspecified || Kind == dateTime.Kind)
            {
                return dateTime;
            }

            // Convert UTC input to local time
            if (Kind == DateTimeKind.Local && dateTime.Kind == DateTimeKind.Utc)
            {
                return dateTime.ToLocalTime();
            }

            // Convert local input to UTC
            if (Kind == DateTimeKind.Utc && dateTime.Kind == DateTimeKind.Local)
            {
                return dateTime.ToUniversalTime();
            }

            // For unspecified input, just re-specify the Kind without actual conversion
            return DateTime.SpecifyKind(dateTime, Kind);
        }
    }
}
