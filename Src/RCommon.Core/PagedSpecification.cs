using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    public class PagedSpecification<T> : Specification<T>, IPagedSpecification<T>
    {
        public PagedSpecification(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderByExpression, bool orderByAscending, int pageIndex, int pageSize) : base(predicate)
        {
            OrderByExpression = orderByExpression;
            OrderByAscending = orderByAscending;
            PageIndex = pageIndex;
            PageSize = pageSize;
        }

        public Expression<Func<T, object>> OrderByExpression { get; }
        public int PageIndex { get;  }
        public int PageSize { get; }

        public bool OrderByAscending { get; set; }
    }
}
