
using System.Runtime.Serialization;

namespace RCommon.Models
{
    /// <summary>
    /// Abstract base record providing default pagination and sorting parameters.
    /// Derive from this class to create concrete paginated list request types.
    /// </summary>
    /// <remarks>
    /// Defaults: page 1, 20 items per page, sorted by "id" with no sort direction.
    /// </remarks>
    /// <seealso cref="IPaginatedListRequest"/>
    /// <seealso cref="SearchPaginatedListRequest"/>
    [DataContract]
    public abstract record PaginatedListRequest : IPaginatedListRequest
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PaginatedListRequest"/> with default values:
        /// page 1, page size 20, sort by "id", and no sort direction.
        /// </summary>
        public PaginatedListRequest()
        {
            PageNumber = 1;
            PageSize = 20;
            SortBy = "id";
            SortDirection = SortDirectionEnum.None;
        }

        /// <inheritdoc />
        [DataMember]
        public virtual int PageNumber { get; set; }

        /// <inheritdoc />
        [DataMember]
        public virtual int PageSize { get; set; }

        /// <inheritdoc />
        [DataMember]
        public virtual string? SortBy { get; set; }

        /// <inheritdoc />
        [DataMember]
        public virtual SortDirectionEnum SortDirection { get; set; }
    }
}
