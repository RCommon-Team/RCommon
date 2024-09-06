using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using RCommon.Collections;

namespace RCommon.Models
{
    /// <summary>
    /// Represents a Data Transfer Object (DTO) that is typically used to encapsulate a PaginatedList so that it can be
    /// delivered to the application layer. This should be an immutable object.
    /// </summary>
    public abstract record PaginatedListModel<TSource, TOut> : IModel
        where TSource : class
        where TOut : class
    {

        private Expression<Func<TSource, object>> _sortExpression = null;

        protected PaginatedListModel(IQueryable<TSource> source, PaginatedListRequest paginatedListRequest, bool skipTotal = false)
        {
            PaginateQueryable(source, paginatedListRequest, skipTotal);
        }

        /// <summary>
        /// Accepts <see cref="IPaginatedList{T}"/> and overrides total record count with the total count provided in the parameter. 
        /// </summary>
        /// <param name="source">Pre-filtered list of data.</param>
        /// <param name="paginatedListRequest">Request model that contains populated state date for page number, page size, sort by, etc.</param>
        /// <param name="totalCount">Total count of records contained by pre-filtered data source. This should be different than the actual record 
        /// count in <see cref="IPaginatedList{T}"/></param>
        /// <param name="skipSort">Instructions on whether or not to skip sorting. Default is false</param>
        /// <remarks></remarks>
        protected PaginatedListModel(IPaginatedList<TSource> source, PaginatedListRequest paginatedListRequest, int totalCount, bool skipSort = false)
        {
            PaginateList(source, paginatedListRequest, totalCount, skipSort);
        }

        private IQueryable<TSource> Sort(IQueryable<TSource> source)
        {

            if (this.SortExpression == null)
            {
                return source;
            }

            return SortDirection == SortDirectionEnum.Descending
                ? source.OrderByDescending(this.SortExpression)
                : source.OrderBy(this.SortExpression);
        }

        private IList<TSource> Sort(IPaginatedList<TSource> source)
        {
            
            if (this.SortExpression == null)
            {
                return source;
            }

            var sortFunc = this.SortExpression.Compile(); // How much overhead is this?

            var list = SortDirection == SortDirectionEnum.Descending
                ? source.OrderByDescending(sortFunc).ToList()
                : source.OrderBy(sortFunc).ToList();
            return list;
        }

        protected void PaginateQueryable(IQueryable<TSource> source, PaginatedListRequest paginatedListRequest, bool skipTotal = false, bool skipSort = false)
        {
            if (paginatedListRequest == null)
            {
                return;
            }
            Guard.IsNotNull(source, nameof(source));

            SortBy = paginatedListRequest.SortBy ?? "id";
            SortDirection = paginatedListRequest.SortDirection;

            PageSize = paginatedListRequest.PageSize;
            PageNumber = paginatedListRequest.PageNumber;

            if (!skipTotal)
            {
                TotalCount = source.Count();
                TotalPages = TotalCount / PageSize + (TotalCount % PageSize > 0 ? 1 : 0) ?? 1;
            }

            var query = skipSort ? source : Sort(source);

            if (PageSize.HasValue)
            {
                query = query.Skip(PageSize.Value * (PageNumber - 1)).Take(PageSize.Value);
            }

            Items = CastItems(query).ToList();
        }

        protected void PaginateList(IPaginatedList<TSource> source, PaginatedListRequest paginatedListRequest, int totalCount, 
            bool skipSort = false)
        {
            if (paginatedListRequest == null)
            {
                return;
            }
            Guard.IsNotNull(source, nameof(source));

            SortBy = paginatedListRequest.SortBy ?? "id";
            SortDirection = paginatedListRequest.SortDirection;

            PageSize = paginatedListRequest.PageSize;
            PageNumber = paginatedListRequest.PageNumber;

            if (totalCount > 0)
            {
                TotalCount = totalCount;
                TotalPages = TotalCount / PageSize + (TotalCount % PageSize > 0 ? 1 : 0) ?? 1;
            }

            var query = skipSort ? source : Sort(source);

            /*if (PageSize.HasValue) // No need to implement paging as list should contain current view
            {
                query = query.Skip(PageSize.Value * (PageIndex - 1)).Take(PageSize.Value).ToList();
            }*/

            Items = CastItems(query.AsQueryable()).ToList();
        }

        protected abstract IQueryable<TOut> CastItems(IQueryable<TSource> source);

        public virtual Expression<Func<TSource, object>> SortExpression { get => _sortExpression; set => _sortExpression = value; }

        public List<TOut> Items { get; set; }

        public int? PageSize { get; set; }
        public int PageNumber { get; set; }

        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        public string SortBy { get; set; }

        public SortDirectionEnum SortDirection { get; set; }

        public bool HasPreviousPage
        {
            get
            {
                return (PageNumber > 1);
            }
        }

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
    public abstract record PaginatedListModel<TSource> : IModel
        where TSource : class
    {

        private Expression<Func<TSource, object>> _sortExpression = null;

        protected PaginatedListModel(IQueryable<TSource> source, PaginatedListRequest paginatedListRequest, bool skipTotal = false)
        {
            PaginateQueryable(source, paginatedListRequest, skipTotal);
        }

        /// <summary>
        /// Accepts <see cref="IPaginatedList{T}"/> and overrides total record count with the total count provided in the parameter. 
        /// </summary>
        /// <param name="source">Pre-filtered list of data.</param>
        /// <param name="paginatedListRequest">Request model that contains populated state date for page number, page size, sort by, etc.</param>
        /// <param name="totalCount">Total count of records contained by pre-filtered data source. This should be different than the actual record 
        /// count in <see cref="IPaginatedList{T}"/></param>
        /// <param name="skipSort">Instructions on whether or not to skip sorting. Default is false</param>
        /// <remarks></remarks>
        protected PaginatedListModel(IPaginatedList<TSource> source, PaginatedListRequest paginatedListRequest, int totalCount, bool skipSort = false)
        {
            PaginateList(source, paginatedListRequest, totalCount, skipSort);
        }

        private IQueryable<TSource> Sort(IQueryable<TSource> source)
        {

            if (this.SortExpression == null)
            {
                return source;
            }

            return SortDirection == SortDirectionEnum.Descending
                ? source.OrderByDescending(this.SortExpression)
                : source.OrderBy(this.SortExpression);
        }

        private IList<TSource> Sort(IPaginatedList<TSource> source)
        {

            if (this.SortExpression == null)
            {
                return source;
            }

            var sortFunc = this.SortExpression.Compile(); // How much overhead is this?

            var list = SortDirection == SortDirectionEnum.Descending
                ? source.OrderByDescending(sortFunc).ToList()
                : source.OrderBy(sortFunc).ToList();
            return list;
        }

        protected void PaginateQueryable(IQueryable<TSource> source, PaginatedListRequest paginatedListRequest, bool skipTotal = false, bool skipSort = false)
        {
            if (paginatedListRequest == null)
            {
                return;
            }
            Guard.IsNotNull(source, nameof(source));

            SortBy = paginatedListRequest.SortBy ?? "id";
            SortDirection = paginatedListRequest.SortDirection;

            PageSize = paginatedListRequest.PageSize;
            PageNumber = paginatedListRequest.PageNumber;

            if (!skipTotal)
            {
                TotalCount = source.Count();
                TotalPages = TotalCount / PageSize + (TotalCount % PageSize > 0 ? 1 : 0) ?? 1;
            }

            var query = skipSort ? source : Sort(source);

            if (PageSize.HasValue)
            {
                query = query.Skip(PageSize.Value * (PageNumber - 1)).Take(PageSize.Value);
            }

            Items = query.ToList();
        }

        protected void PaginateList(IPaginatedList<TSource> source, PaginatedListRequest paginatedListRequest, int totalCount,
            bool skipSort = false)
        {
            if (paginatedListRequest == null)
            {
                return;
            }
            Guard.IsNotNull(source, nameof(source));

            SortBy = paginatedListRequest.SortBy ?? "id";
            SortDirection = paginatedListRequest.SortDirection;

            PageSize = paginatedListRequest.PageSize;
            PageNumber = paginatedListRequest.PageNumber;

            if (totalCount > 0)
            {
                TotalCount = totalCount;
                TotalPages = TotalCount / PageSize + (TotalCount % PageSize > 0 ? 1 : 0) ?? 1;
            }

            var query = skipSort ? source : Sort(source);

            /*if (PageSize.HasValue) // No need to implement paging as list should contain current view
            {
                query = query.Skip(PageSize.Value * (PageIndex - 1)).Take(PageSize.Value).ToList();
            }*/

            Items = query.AsQueryable().ToList();
        }

        public virtual Expression<Func<TSource, object>> SortExpression { get => _sortExpression; set => _sortExpression = value; }

        public List<TSource> Items { get; set; }

        public int? PageSize { get; set; }
        public int PageNumber { get; set; }

        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        public string SortBy { get; set; }

        public SortDirectionEnum SortDirection { get; set; }

        public bool HasPreviousPage
        {
            get
            {
                return (PageNumber > 1);
            }
        }

        public bool HasNextPage
        {
            get
            {
                return (PageNumber < TotalPages);
            }
        }
    }
}
