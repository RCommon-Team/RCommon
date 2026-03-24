using System.Collections.Generic;

namespace RCommon.Models;

public interface IPagedResult<T>
{
    IReadOnlyList<T> Items { get; }
    long TotalCount { get; }
    int PageNumber { get; }
    int PageSize { get; }
    int TotalPages { get; }
    bool HasNextPage { get; }
    bool HasPreviousPage { get; }
}
