namespace RCommon.Domain.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    public class EagerFetchingStrategy<T>
    {
        private IList<Expression> _paths;

        public EagerFetchingStrategy()
        {
            this._paths = new List<Expression>();
        }

        public EagerFetchingPath<TChild> Fetch<TChild>(Expression<Func<T, object>> path)
        {
            this._paths.Add(path);
            return new EagerFetchingPath<TChild>(this._paths);
        }

        public IEnumerable<Expression> Paths =>
            this._paths.ToArray<Expression>();
    }
}

