using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.Domain.Repositories
{
    public abstract class ObjectSourceLifetimeManager<T> : DisposableResource
    {
        protected ObjectSourceLifetimeManager()
        {
        }

        public abstract IDictionary<string, T> GetAllObjectSources();
        public abstract T GetObjectSource(Type type);
        public abstract void RegisterObjectSource(Func<T> objectSource);

        public abstract void ClearObjectSources();

        public abstract IDictionary<string, T> GetOpenSessions();

        public abstract void RemoveObjectSource(string name);
    }
}
