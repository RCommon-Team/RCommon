using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using RCommon.Extensions;
using System.Text.Json.Serialization;
using RCommon.Serialization.Json;

namespace RCommon.Models
{
    /// <summary>
    /// Represents a Data Transfer Object (DTO) that is typically used to encapsulate a PaginatedList so that it can be
    /// delivered to the application layer. This should be an immutable object.
    /// </summary>
    public abstract record PaginatedListModel<TSource, TOut> : IModel
        where TSource : class, new()
    {
        protected PaginatedListModel()
        {

        }

        protected PaginatedListModel(IQueryable<TSource> source, PaginatedListRequest paginatedListRequest, bool skipTotal = false)
        {
            Paginate(source, paginatedListRequest, skipTotal);
        }

        private IQueryable<TSource> Sort(IQueryable<TSource> source)
        {
            var sortExp = SortExpression();

            if (sortExp == null)
            {
                return source;
            }

            return SortDirection == SortDirectionEnum.Descending
                ? source.OrderByDescending(sortExp)
                : source.OrderBy(sortExp);
        }

        protected void Paginate(IQueryable<TSource> source, PaginatedListRequest paginatedListRequest, bool skipTotal = false, 
            int totalRows = 0, bool skipSort = false)
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

            if (totalRows > 0)
            {
                TotalCount = totalRows;
                TotalPages = TotalCount / PageSize + (TotalCount % PageSize > 0 ? 1 : 0) ?? 1;
            }

            var query = skipSort ? source : Sort(source);

            if (PageSize.HasValue && totalRows == 0)
            {
                query = query.Skip(PageSize.Value * (PageIndex - 1)).Take(PageSize.Value);
            }

            Items = CastItems(query).ToList();
        }

        protected virtual Expression<Func<TSource, object>> SortExpression() => null;

        protected abstract IQueryable<TOut> CastItems(IQueryable<TSource> source);

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
