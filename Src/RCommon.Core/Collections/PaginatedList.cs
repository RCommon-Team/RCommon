using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCommon.Collections
{
    /// <summary>
    /// A concrete implementation of <see cref="IPaginatedList{T}"/> that extends <see cref="List{T}"/>
    /// to provide pagination over a data source. Supports construction from <see cref="IQueryable{T}"/>,
    /// <see cref="IList{T}"/>, and <see cref="ICollection{T}"/> sources.
    /// </summary>
    /// <typeparam name="T">The type of elements in the paginated list.</typeparam>
    public class PaginatedList<T> : List<T>, IPaginatedList<T>
    {
        /// <summary>
        /// Initializes a new empty instance of <see cref="PaginatedList{T}"/>.
        /// </summary>
        public PaginatedList()
        {

        }

        /// <summary>Gets the 1-based index of the current page.</summary>
        public int PageIndex { get; private set; }

        /// <summary>Gets the maximum number of items per page.</summary>
        public int PageSize { get; private set; }

        /// <summary>Gets the total number of items across all pages.</summary>
        public int TotalCount { get; private set; }

        /// <summary>Gets the total number of pages.</summary>
        public int TotalPages { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="PaginatedList{T}"/> from an <see cref="IQueryable{T}"/> source.
        /// </summary>
        /// <param name="source">The queryable data source to paginate.</param>
        /// <param name="pageIndex">The 1-based page index, or null to default to the first page.</param>
        /// <param name="pageSize">The number of items per page.</param>
        public PaginatedList(IQueryable<T> source, int? pageIndex, int pageSize)
        {
            PageIndex = pageIndex ?? 1;
            PageSize = pageSize;
            TotalCount = source.Count();
            // Calculate total pages using integer division, ensuring at least 1 page
            TotalPages = ((TotalCount - 1) / PageSize) + 1;

            this.AddRange(source.Skip((PageIndex - 1) * PageSize).Take(PageSize));
        }

        /// <summary>
        /// Initializes a new instance of <see cref="PaginatedList{T}"/> from an <see cref="IList{T}"/> source.
        /// </summary>
        /// <param name="source">The list data source to paginate.</param>
        /// <param name="pageIndex">The 1-based page index, or null to default to the first page.</param>
        /// <param name="pageSize">The number of items per page.</param>
        public PaginatedList(IList<T> source, int? pageIndex, int pageSize)
        {
            PageIndex = pageIndex ?? 1;
            PageSize = pageSize;
            TotalCount = source.Count();
            TotalPages = ((TotalCount - 1) / PageSize) + 1;

            this.AddRange(source.Skip((PageIndex - 1) * PageSize).Take(PageSize));
        }

        /// <summary>
        /// Initializes a new instance of <see cref="PaginatedList{T}"/> from an <see cref="ICollection{T}"/> source.
        /// </summary>
        /// <param name="source">The collection data source to paginate.</param>
        /// <param name="pageIndex">The 1-based page index, or null to default to the first page.</param>
        /// <param name="pageSize">The number of items per page.</param>
        public PaginatedList(ICollection<T> source, int? pageIndex, int pageSize)
        {
            PageIndex = pageIndex ?? 1;
            PageSize = pageSize;
            TotalCount = source.Count();

            TotalPages = ((TotalCount - 1) / PageSize) + 1;

            this.AddRange(source.Skip((PageIndex - 1) * PageSize).Take(PageSize));
        }

        /// <summary>
        /// Gets a value indicating whether there is a previous page (i.e., <see cref="PageIndex"/> is greater than 1).
        /// </summary>
        public bool HasPreviousPage
        {
            get
            {
                return (PageIndex > 1);
            }
        }

        /// <summary>
        /// Gets a value indicating whether there is a next page (i.e., <see cref="PageIndex"/> is less than <see cref="TotalPages"/>).
        /// </summary>
        public bool HasNextPage
        {
            get
            {
                return (PageIndex < TotalPages);
            }
        }
    }
}
