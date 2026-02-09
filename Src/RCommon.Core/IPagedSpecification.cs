using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    /// <summary>
    /// Extends <see cref="ISpecification{T}"/> to add paging and ordering support for query specifications.
    /// </summary>
    /// <typeparam name="T">The entity type that the specification applies to.</typeparam>
    /// <seealso cref="PagedSpecification{T}"/>
    public interface IPagedSpecification<T> : ISpecification<T>
    {
        /// <summary>
        /// Gets the 1-based page number to retrieve.
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// Gets the number of items per page.
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// Gets the expression used to determine the sort order of results.
        /// </summary>
        public Expression<Func<T, object>> OrderByExpression { get; }

        /// <summary>
        /// Gets or sets a value indicating whether results should be sorted in ascending order.
        /// When <c>false</c>, results are sorted in descending order.
        /// </summary>
        public bool OrderByAscending { get; set; }
    }
}
