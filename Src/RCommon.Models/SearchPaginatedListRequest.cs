using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Models
{
    /// <summary>
    /// A paginated list request that includes a free-text search filter.
    /// Combines pagination/sorting from <see cref="PaginatedListRequest"/> with
    /// the search capability defined by <see cref="ISearchPaginatedListRequest"/>.
    /// </summary>
    /// <seealso cref="PaginatedListRequest"/>
    /// <seealso cref="ISearchPaginatedListRequest"/>
    [DataContract]
    public record SearchPaginatedListRequest : PaginatedListRequest, ISearchPaginatedListRequest
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SearchPaginatedListRequest"/>
        /// with default pagination values inherited from <see cref="PaginatedListRequest"/>.
        /// </summary>
        public SearchPaginatedListRequest()
        {

        }

        /// <inheritdoc />
        [DataMember]
        public string? SearchString { get; set; }
    }
}
