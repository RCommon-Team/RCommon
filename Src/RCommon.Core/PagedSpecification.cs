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
        public PagedSpecification(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderByExpression, 
            bool orderByAscending, int pageNumber, int pageSize) : base(predicate)
        {
            OrderByExpression = orderByExpression;
            OrderByAscending = orderByAscending;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }

        public Expression<Func<T, object>> OrderByExpression { get; }
        public int PageNumber { get;  }
        public int PageSize { get; }

        public bool OrderByAscending { get; set; }
    }
}
