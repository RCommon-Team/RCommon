namespace RCommon.Models
{
    /// <summary>
    /// Extends paginated list requests with a free-text search capability.
    /// Implementations combine search filtering with pagination and sorting.
    /// </summary>
    /// <seealso cref="SearchPaginatedListRequest"/>
    /// <seealso cref="IPaginatedListRequest"/>
    public interface ISearchPaginatedListRequest : IModel
    {
        /// <summary>
        /// Gets or sets the search string used to filter results.
        /// </summary>
        string? SearchString { get; set; }
    }
}
