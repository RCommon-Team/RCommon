using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace RCommon.Expressions
{
    public class SortingExpression<T>
    {
        private readonly Expression<Func<T, object>> _expression;
        private readonly SortDirection _direction;

        public SortingExpression(Expression<Func<T, object>> expression, SortDirection direction)
        {
            this._expression = expression;
            this._direction = direction;
        }

        public Expression<Func<T, object>> SortExpression => _expression;

        public SortDirection SortDirection => _direction;
    }
}
