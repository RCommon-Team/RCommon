using System;

namespace RCommon
{
    public class SystemTimeOptions
    {
        /// <summary>
        /// Default: <see cref="DateTimeKind.Unspecified"/>
        /// </summary>
        public DateTimeKind Kind { get; set; }

        public SystemTimeOptions()
        {
            Kind = DateTimeKind.Unspecified;
        }
    }
}