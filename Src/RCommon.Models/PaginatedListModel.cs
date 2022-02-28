using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using RCommon.Extensions;
using System.Text.Json.Serialization;
using RCommon.Serialization.Json;
using RCommon.Collections;

namespace RCommon.Models
{
    /// <summary>
    /// Represents a Data Transfer Object (DTO) that is typically used to encapsulate a PaginatedList so that it can be
    /// delivered to the application layer. This should be an immutable object.
    /// </summary>
    public abstract record PaginatedListModel<TSource, TOut> : IModel
        where TSource : class, new()
        where TOut : class, new()
    {

        private Expression<Func<TSource, object>> _sortExpression = null;

        protected PaginatedListModel(IQueryable<TSource> source, PaginatedListRequest paginatedListRequest, bool skipTotal = false)
        {
            PaginateQueryable(source, paginatedListRequest, skipTotal);
        }
        protected PaginatedListModel(IPaginatedList<TSource> source, PaginatedListRequest paginatedListRequest, int totalCount, int totalPages,
            bool skipTotal = false, bool skipSort = false)
        {
            PaginateList(source, paginatedListRequest, totalCount, totalPages, skipTotal, skipSort);
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
            PageIndex = paginatedListRequest.PageIndex;

            if (!skipTotal)
            {
                TotalCount = source.Count();
                TotalPages = TotalCount / PageSize + (TotalCount % PageSize > 0 ? 1 : 0) ?? 1;
            }

            var query = skipSort ? source : Sort(source);

            if (PageSize.HasValue)
            {
                query = query.Skip(PageSize.Value * (PageIndex - 1)).Take(PageSize.Value);
            }

            Items = CastItems(query).ToList();
        }

        protected void PaginateList(IPaginatedList<TSource> source, PaginatedListRequest paginatedListRequest, int totalCount, int totalPages, 
            bool skipTotal = false, bool skipSort = false)
        {
            if (paginatedListRequest == null)
            {
                return;
            }
            Guard.IsNotNull(source, nameof(source));

            SortBy = paginatedListRequest.SortBy ?? "id";
            SortDirection = paginatedListRequest.SortDirection;

            PageSize = paginatedListRequest.PageSize;
            PageIndex = paginatedListRequest.PageIndex;

            if (!skipTotal)
            {
                TotalCount = totalCount;
                TotalPages = TotalCount / PageSize + (TotalCount % PageSize > 0 ? 1 : 0) ?? 1;
            }

            if (totalCount > 0)
            {
                TotalCount = totalCount;
                TotalPages = TotalCount / PageSize + (TotalCount % PageSize > 0 ? 1 : 0) ?? 1;
            }

            var query = skipSort ? source.AsQueryable() : Sort(source.AsQueryable());

            if (PageSize.HasValue)
            {
                query = query.Skip(PageSize.Value * (PageIndex - 1)).Take(PageSize.Value);
            }

            Items = CastItems(query).ToList();
        }

        protected abstract IQueryable<TOut> CastItems(IQueryable<TSource> source);

        [JsonIgnore]
        public virtual Expression<Func<TSource, object>> SortExpression { get => _sortExpression; set => _sortExpression = value; }

        public List<TOut> Items { get; set; }

        public int? PageSize { get; set; }
        public int PageIndex { get; set; }

        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        public string SortBy { get; set; }

        [JsonConverter(typeof(JsonByteEnumConverter<SortDirectionEnum>))]
        public SortDirectionEnum SortDirection { get; set; }

        public bool HasPreviousPage
        {
            get
            {
                return (PageIndex > 1);
            }
        }

        public bool HasNextPage
        {
            get
            {
                return (PageIndex < TotalPages);
            }
        }
    }
}
