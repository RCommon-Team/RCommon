namespace RCommon.Models
{
    /// <summary>
    /// Defines the contract for a paginated list request, specifying paging and sorting parameters
    /// used to retrieve a subset of data from a larger collection.
    /// </summary>
    /// <seealso cref="PaginatedListRequest"/>
    /// <seealso cref="PaginatedListModel{TSource}"/>
    public interface IPaginatedListRequest : IModel
    {
        /// <summary>
        /// Gets or sets the one-based page number to retrieve.
        /// </summary>
        int PageNumber { get; set; }

        /// <summary>
        /// Gets or sets the number of items per page.
        /// </summary>
        int PageSize { get; set; }

        /// <summary>
        /// Gets or sets the name of the property to sort by.
        /// </summary>
        string? SortBy { get; set; }

        /// <summary>
        /// Gets or sets the sort direction (ascending, descending, or none).
        /// </summary>
        /// <seealso cref="SortDirectionEnum"/>
        SortDirectionEnum SortDirection { get; set; }
    }
}
