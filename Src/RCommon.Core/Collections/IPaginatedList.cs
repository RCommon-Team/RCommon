using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCommon.Collections
{
    /// <summary>
    /// Represents a paginated list that extends <see cref="IList{T}"/> with pagination metadata
    /// such as page index, page size, total count, and navigation flags.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    public interface IPaginatedList<T> : IList<T>
    {
        /// <summary>Gets a value indicating whether there is a next page available.</summary>
        bool HasNextPage { get; }

        /// <summary>Gets a value indicating whether there is a previous page available.</summary>
        bool HasPreviousPage { get; }

        /// <summary>Gets the 1-based index of the current page.</summary>
        int PageIndex { get; }

        /// <summary>Gets the maximum number of items per page.</summary>
        int PageSize { get; }

        /// <summary>Gets the total number of items across all pages.</summary>
        int TotalCount { get; }

        /// <summary>Gets the total number of pages.</summary>
        int TotalPages { get; }
    }
}
