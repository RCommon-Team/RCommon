

using System;

namespace RCommon
{
    /// <summary>
    /// Default implementation of <see cref="ISystemTimeOptions"/> providing configuration
    /// for the <see cref="SystemTime"/> service.
    /// </summary>
    public class SystemTimeOptions : ISystemTimeOptions
    {
        /// <summary>
        /// Gets or sets the <see cref="DateTimeKind"/> that controls system time behavior.
        /// Default: <see cref="DateTimeKind.Unspecified"/>.
        /// </summary>
        public DateTimeKind Kind { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="SystemTimeOptions"/> with <see cref="DateTimeKind.Unspecified"/> as the default kind.
        /// </summary>
        public SystemTimeOptions()
        {
            Kind = DateTimeKind.Unspecified;
        }
    }
}
