namespace RCommon.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;

    public class EagerFetchingPath<T> : IEagerFetchingPath<T>
    {
        private readonly IList<Expression> _paths;

        public EagerFetchingPath(IList<Expression> paths)
        {
            this._paths = paths;
        }

        public IEagerFetchingPath<TChild> And<TChild>(Expression<Func<T, object>> path)
        {
            this._paths.Add(path);
            return new EagerFetchingPath<TChild>(this._paths);
        }
    }
}

