using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace RCommon
{
    /// <summary>
    /// Provides commonly used application-wide constant values for domain object identifiers,
    /// versioning, delimiters, and paging defaults.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Gets the current thread's culture information.
        /// </summary>
        public static CultureInfo CurrentCulture
        {
            get
            {
                return CultureInfo.CurrentCulture;
            }
        }

        /// <summary>
        /// Default identifier value for a domain object that has not yet been persisted.
        /// </summary>
        public const int IdOfUnsavedDomainObject = 0;

        /// <summary>
        /// Sentinel identifier used for stub/mock domain objects in testing scenarios.
        /// </summary>
        public const int IdOfStubDomainObject = -999999999;  // used as the Id of Stub objects, should be renamed to IdOfStubObject

        /// <summary>
        /// Special index value indicating a non-domain object (e.g., artificial object types
        /// such as ObjectDescriptor, ObjectAndTypeElement, or None-Entry items for combo boxes).
        /// </summary>
        public const int IndexOfStubDomainObject = -1000;  // a special Id to indicate a "Not-A-Domain-Object" (e.g.,

        // artificial object types such as: ObjectDescriptor, ObjectAndTypeElement, None-Entry Item (for combobox)

        /// <summary>
        /// Represents an invalid or uninitialized version number for optimistic concurrency control.
        /// </summary>
        public const int InvalidVersion = -1;

        /// <summary>
        /// The index of the first real (non-stub) object in a data list, typically following a placeholder entry.
        /// </summary>
        public static readonly int IndexOfFirstNonStubObjectInDataList = 1;

        /// <summary>
        /// The default delimiter character used for string splitting and joining operations.
        /// </summary>
        public static readonly char DefaultDelimiter = ',';

        /// <summary>
        /// The default number of items per page for paged queries.
        /// </summary>
        public static readonly int DefaultPageSize = 10;

        /// <summary>
        /// The default property name used to resolve singleton instances.
        /// </summary>
        public static readonly string DefaultSingletonPropertyName = "Singleton";

        /// <summary>
        /// The default property name used for optimistic concurrency version tracking.
        /// </summary>
        public static readonly string DefaultVersionPropertyName = "ObjectVersion";
    }
}


