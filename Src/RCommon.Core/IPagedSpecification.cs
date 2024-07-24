using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    public interface IPagedSpecification<T> : ISpecification<T>
    {
        public int PageNumber { get; }
        public int PageSize { get; }

        public Expression<Func<T, object>> OrderByExpression { get; }

        public bool OrderByAscending { get; set; }
    }
}
