using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;

namespace RCommon.Models
{
    /// <summary>
    /// Represents a Data Transfer Object (DTO) that is typically used to encapsulate a PaginatedList so that it can be
    /// delivered to the application layer. This should be an immutable object.
    /// </summary>
    /// <typeparam name="TSource">The source entity type from the data layer.</typeparam>
    /// <typeparam name="TOut">The output/projected type exposed to consumers (e.g., a view model or DTO).</typeparam>
    /// <remarks>
    /// This two-type-parameter variant allows projecting source entities into a different output type
    /// via the abstract <see cref="CastItems"/> method.
    /// </remarks>
    [DataContract]
    public abstract record PaginatedListModel<TSource, TOut> : IModel
        where TSource : class
        where TOut : class
    {

        /// <summary>
        /// Initializes a new instance of <see cref="PaginatedListModel{TSource, TOut}"/>
        /// by paginating the provided queryable source.
        /// </summary>
        /// <param name="source">The queryable data source to paginate.</param>
        /// <param name="paginatedListRequest">The request containing paging and sorting parameters.</param>
        protected PaginatedListModel(IQueryable<TSource> source, PaginatedListRequest paginatedListRequest)
        {
            PaginateQueryable(source, paginatedListRequest);
        }

        /// <summary>
        /// Applies pagination and sorting parameters to the source queryable and populates the model properties.
        /// </summary>
        /// <param name="source">The queryable data source to paginate. Must not be <c>null</c>.</param>
        /// <param name="paginatedListRequest">The request containing paging and sorting parameters. Must not be <c>null</c>.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="source"/> or <paramref name="paginatedListRequest"/> is <c>null</c>.</exception>
        protected void PaginateQueryable(IQueryable<TSource> source, PaginatedListRequest paginatedListRequest)
        {
            if (source == null)
            {
                throw new ArgumentException("Source Data cannot be null");
            }

            if (paginatedListRequest == null)
            {
                throw new ArgumentException("Request input cannot be null");
            }

            // Default sort field to "id" when none is specified.
            SortBy = paginatedListRequest.SortBy ?? "id";
            SortDirection = paginatedListRequest.SortDirection;
            PageSize = paginatedListRequest.PageSize;
            PageNumber = paginatedListRequest.PageNumber;
            TotalCount = source.Count();

            // Calculate total pages using ceiling division: add 1 if there is a remainder.
            TotalPages = TotalCount / PageSize + (TotalCount % PageSize > 0 ? 1 : 0);

            // Skip to the requested page (one-based) and take only the page size number of items.
            var query = source.Skip(PageSize * (PageNumber - 1)).Take(PageSize);

            // Project source items to the output type via the abstract CastItems method.
            Items = CastItems(query).ToList();
        }

        /// <summary>
        /// When implemented in a derived class, projects source entities to the output type.
        /// </summary>
        /// <param name="source">The queryable containing the current page of source entities.</param>
        /// <returns>A queryable of projected output items.</returns>
        protected abstract IQueryable<TOut> CastItems(IQueryable<TSource> source);

        /// <summary>
        /// Gets or sets the list of items for the current page, projected to <typeparamref name="TOut"/>.
        /// </summary>
        [DataMember]
        public List<TOut> Items { get; set; } = new List<TOut>();

        /// <summary>
        /// Gets or sets the number of items per page.
        /// </summary>
        [DataMember]
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets the one-based page number.
        /// </summary>
        [DataMember]
        public int PageNumber { get; set; }

        /// <summary>
        /// Gets or sets the total number of pages available.
        /// </summary>
        [DataMember]
        public int TotalPages { get; set; }

        /// <summary>
        /// Gets or sets the total count of items across all pages.
        /// </summary>
        [DataMember]
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the name of the property used for sorting.
        /// </summary>
        [DataMember]
        public string? SortBy { get; set; }

        /// <summary>
        /// Gets or sets the sort direction applied to the results.
        /// </summary>
        /// <seealso cref="SortDirectionEnum"/>
        [DataMember]
        public SortDirectionEnum SortDirection { get; set; }

        /// <summary>
        /// Gets a value indicating whether there is a page before the current page.
        /// </summary>
        [DataMember]
        public bool HasPreviousPage
        {
            get
            {
                return (PageNumber > 1);
            }
        }

        /// <summary>
        /// Gets a value indicating whether there is a page after the current page.
        /// </summary>
        [DataMember]
        public bool HasNextPage
        {
            get
            {
                return (PageNumber < TotalPages);
            }
        }
    }

    /// <summary>
    /// Represents a Data Transfer Object (DTO) that is typically used to encapsulate a PaginatedList so that it can be
    /// delivered to the application layer. This should be an immutable object.
    /// </summary>
    /// <typeparam name="TSource">The entity type contained in the paginated list.</typeparam>
    /// <remarks>
    /// This single-type-parameter variant returns items in their original source type
    /// without projection. For source-to-output projection, use
    /// <see cref="PaginatedListModel{TSource, TOut}"/> instead.
    /// </remarks>
    [DataContract]
    public abstract record PaginatedListModel<TSource> : IModel
        where TSource : class
    {

        /// <summary>
        /// Initializes a new instance of <see cref="PaginatedListModel{TSource}"/>
        /// by paginating the provided queryable source.
        /// </summary>
        /// <param name="source">The queryable data source to paginate.</param>
        /// <param name="paginatedListRequest">The request containing paging and sorting parameters.</param>
        protected PaginatedListModel(IQueryable<TSource> source, PaginatedListRequest paginatedListRequest)
        {
            PaginateQueryable(source, paginatedListRequest);
        }

        /// <summary>
        /// Applies pagination and sorting parameters to the source queryable and populates the model properties.
        /// </summary>
        /// <param name="source">The queryable data source to paginate. Must not be <c>null</c>.</param>
        /// <param name="paginatedListRequest">The request containing paging and sorting parameters. Must not be <c>null</c>.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="source"/> or <paramref name="paginatedListRequest"/> is <c>null</c>.</exception>
        protected void PaginateQueryable(IQueryable<TSource> source, PaginatedListRequest paginatedListRequest)
        {
            if (source == null)
            {
                throw new ArgumentException("Source Data cannot be null");
            }

            if (paginatedListRequest == null)
            {
                throw new ArgumentException("Request input cannot be null");
            }

            // Default sort field to "id" when none is specified.
            SortBy = paginatedListRequest.SortBy ?? "id";
            SortDirection = paginatedListRequest.SortDirection;
            PageSize = paginatedListRequest.PageSize;
            PageNumber = paginatedListRequest.PageNumber;
            TotalCount = source.Count();

            // Calculate total pages using ceiling division: add 1 if there is a remainder.
            TotalPages = TotalCount / PageSize + (TotalCount % PageSize > 0 ? 1 : 0);

            // Skip to the requested page (one-based) and take only the page size number of items.
            Items = source.Skip(PageSize * (PageNumber - 1)).Take(PageSize).ToList();
        }

        /// <summary>
        /// Gets or sets the list of items for the current page.
        /// </summary>
        [DataMember]
        public List<TSource> Items { get; set; } = new List<TSource>();

        /// <summary>
        /// Gets or sets the number of items per page.
        /// </summary>
        [DataMember]
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets the one-based page number.
        /// </summary>
        [DataMember]
        public int PageNumber { get; set; }

        /// <summary>
        /// Gets or sets the total number of pages available.
        /// </summary>
        [DataMember]
        public int TotalPages { get; set; }

        /// <summary>
        /// Gets or sets the total count of items across all pages.
        /// </summary>
        [DataMember]
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the name of the property used for sorting.
        /// </summary>
        [DataMember]
        public string? SortBy { get; set; }

        /// <summary>
        /// Gets or sets the sort direction applied to the results.
        /// </summary>
        /// <seealso cref="SortDirectionEnum"/>
        [DataMember]
        public SortDirectionEnum SortDirection { get; set; }

        /// <summary>
        /// Gets a value indicating whether there is a page before the current page.
        /// </summary>
        [DataMember]
        public bool HasPreviousPage
        {
            get
            {
                return (PageNumber > 1);
            }
        }

        /// <summary>
        /// Gets a value indicating whether there is a page after the current page.
        /// </summary>
        [DataMember]
        public bool HasNextPage
        {
            get
            {
                return (PageNumber < TotalPages);
            }
        }
    }

}
