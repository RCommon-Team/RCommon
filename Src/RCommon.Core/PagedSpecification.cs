using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    /// <summary>
    /// Default implementation of <see cref="IPagedSpecification{T}"/> that combines a filter predicate
    /// with paging and ordering parameters for querying collections of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The entity type that the specification applies to.</typeparam>
    public class PagedSpecification<T> : Specification<T>, IPagedSpecification<T>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PagedSpecification{T}"/> with the specified filter, ordering, and paging parameters.
        /// </summary>
        /// <param name="predicate">The filter expression to select matching entities.</param>
        /// <param name="orderByExpression">The expression used to determine sort order.</param>
        /// <param name="orderByAscending"><c>true</c> for ascending order; <c>false</c> for descending.</param>
        /// <param name="pageNumber">The 1-based page number to retrieve.</param>
        /// <param name="pageSize">The number of items per page.</param>
        public PagedSpecification(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderByExpression,
            bool orderByAscending, int pageNumber, int pageSize) : base(predicate)
        {
            OrderByExpression = orderByExpression;
            OrderByAscending = orderByAscending;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }

        /// <inheritdoc />
        public Expression<Func<T, object>> OrderByExpression { get; }

        /// <inheritdoc />
        public int PageNumber { get;  }

        /// <inheritdoc />
        public int PageSize { get; }

        /// <inheritdoc />
        public bool OrderByAscending { get; set; }
    }
}
