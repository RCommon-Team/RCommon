using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.DataTransferObjects
{
    /// <summary>
    /// Represents a Data Transfer Object (DTO) that is typically used to encapsulate a PaginatedList so that it can be
    /// delivered to an application layer.
    /// </summary>
    public class StaticPaginatedList<T>
        where T : class, new()
    {
        public StaticPaginatedList()
        {

        }

        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        public bool HasPreviousPage
        {
            get;set;
        }

        public bool HasNextPage
        {
            get;set;
        }

        public IEnumerable<T> Data { get; set; }
    }
}
