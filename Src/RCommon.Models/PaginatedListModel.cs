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
    [DataContract]
    public abstract record PaginatedListModel<TSource, TOut> : IModel
        where TSource : class
        where TOut : class
    {

        private Expression<Func<TSource, object>> _sortExpression = null;

        protected PaginatedListModel(IQueryable<TSource> source, PaginatedListRequest paginatedListRequest, bool skipTotal = false)
        {
            PaginateQueryable(source, paginatedListRequest, skipTotal);
        }

        private IQueryable<TSource> Sort(IQueryable<TSource> source)
        {

            if (this._sortExpression == null)
            {
                return source;
            }

            return SortDirection == SortDirectionEnum.Descending
                ? source.OrderByDescending(this._sortExpression)
                : source.OrderBy(this._sortExpression);
        }

        protected void PaginateQueryable(IQueryable<TSource> source, PaginatedListRequest paginatedListRequest, bool skipTotal = false, bool skipSort = false)
        {
            if (source == null)
            {
                throw new ArgumentException("Source Data cannot be null");
            }

            if (paginatedListRequest == null)
            {
                throw new ArgumentException("Request input cannot be null");
            }

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

        protected abstract IQueryable<TOut> CastItems(IQueryable<TSource> source);

        public virtual Expression<Func<TSource, object>> GetSortExpression()
        {
            return _sortExpression;
        }

        [DataMember]
        public List<TOut> Items { get; set; }

        [DataMember]
        public int? PageSize { get; set; }

        [DataMember]
        public int PageNumber { get; set; }

        [DataMember]
        public int TotalPages { get; set; }

        [DataMember]
        public int TotalCount { get; set; }

        [DataMember]
        public string SortBy { get; set; }

        [DataMember]
        public SortDirectionEnum SortDirection { get; set; }

        [DataMember]
        public bool HasPreviousPage
        {
            get
            {
                return (PageNumber > 1);
            }
        }

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
    [DataContract]
    public abstract record PaginatedListModel<TSource> : IModel
        where TSource : class
    {

        private Expression<Func<TSource, object>> _sortExpression = null;

        protected PaginatedListModel(IQueryable<TSource> source, PaginatedListRequest paginatedListRequest, bool skipTotal = false)
        {
            PaginateQueryable(source, paginatedListRequest, skipTotal);
        }

        private IQueryable<TSource> Sort(IQueryable<TSource> source)
        {

            if (this._sortExpression == null)
            {
                return source;
            }

            return SortDirection == SortDirectionEnum.Descending
                ? source.OrderByDescending(this._sortExpression)
                : source.OrderBy(this._sortExpression);
        }

        protected void PaginateQueryable(IQueryable<TSource> source, PaginatedListRequest paginatedListRequest, bool skipTotal = false, bool skipSort = false)
        {
            if (source == null)
            {
                throw new ArgumentException("Source Data cannot be null");
            }

            if (paginatedListRequest == null)
            {
                throw new ArgumentException("Request input cannot be null");
            }

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

        public virtual Expression<Func<TSource, object>> GetSortExpression()
        {
            return _sortExpression;
        }

        [DataMember]
        public List<TSource> Items { get; set; }

        [DataMember]
        public int? PageSize { get; set; }

        [DataMember]
        public int PageNumber { get; set; }

        [DataMember]
        public int TotalPages { get; set; }

        [DataMember]
        public int TotalCount { get; set; }

        [DataMember]
        public string SortBy { get; set; }

        [DataMember]
        public SortDirectionEnum SortDirection { get; set; }

        [DataMember]
        public bool HasPreviousPage
        {
            get
            {
                return (PageNumber > 1);
            }
        }

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
