namespace RCommon.ObjectAccess.EFCore
{
    using Microsoft.EntityFrameworkCore;
    using RCommon.DependencyInjection;
    using RCommon.Domain.Repositories;
    using RCommon.Extensions;
    using RCommon.StateStorage;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    public sealed class EFObjectSourceLifetimeManager : ObjectSourceLifetimeManager<DbContext>
    {
        private IDictionary<string, DbContext> _openSessions;
        private bool _disposed = false;
        private static object _lock = new object();
        IStateStorage _stateStorage;

        public EFObjectSourceLifetimeManager(IStateStorage stateStorage)
        {
            _stateStorage = stateStorage;
        }


        private DbContext AddContextSession(Type type)
        {
            // This is a major hack. We could use GetInstance(key) but it seems MS is wanting to use a factory-based
            // approach so we cannot implement that 
            Debug.WriteLine(this.GetType().ToString() + ": Creating new Context - '" + type.AssemblyQualifiedName + "'");
            var instance = (DbContext)Activator.CreateInstance(Type.GetType(type.AssemblyQualifiedName));
            //var instance = ServiceLocatorWorker.GetInstance<DbContext>(type.AssemblyQualifiedName);

            lock (_lock)
            {
                this.OpenSessions.Add(type.AssemblyQualifiedName, instance);

                _stateStorage.Local.Remove<IDictionary<string, DbContext>>("ObjectContextSessions");
                _stateStorage.Local.Put<IDictionary<string, DbContext>>("ObjectContextSessions", this.OpenSessions);
            }
            return instance;
        }

        public override void ClearObjectSources()
        {
            lock (_lock)
            {
                this.OpenSessions.Clear();
                Debug.WriteLine(base.GetType().ToString() + ": Removing all ObjectContext references");
                _stateStorage.Local.Remove<IDictionary<string, DbContext>>("ObjectContextSessions");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    var openSessions = this.OpenSessions;
                    if (openSessions != null && openSessions.Count > 0)
                    {
                        openSessions.ForEach(session => session.Value.Dispose());
                        openSessions.Clear();
                    }
                }
                this.ClearObjectSources();
                _disposed = true;



            }
        }




        public override IDictionary<string, DbContext> GetOpenSessions()
        {
            IDictionary<string, DbContext> sessions = _stateStorage.Local.Get<IDictionary<string, DbContext>>("ObjectContextSessions");
            if (ReferenceEquals(sessions, null))
            {
                sessions = new Dictionary<string, DbContext>();
            }

            return sessions;
        }

        public override void RemoveObjectSource(string name)
        {
            if (this.OpenSessions.Keys.Contains(name))
            {

                Debug.WriteLine(base.GetType().ToString() + ": Removing Context - '" + name + "'");
                var objA = this.OpenSessions[name] as DbContext;
                if (!ReferenceEquals(objA, null))
                {
                    objA.Dispose();
                }

                this.OpenSessions.Remove(name);

                lock (_lock)
                {
                    _stateStorage.Local.Remove<IDictionary<string, DbContext>>("ObjectContextSessions");
                    _stateStorage.Local.Put<IDictionary<string, DbContext>>("ObjectContextSessions", this.OpenSessions);
                }
            }
        }

        public override IDictionary<string, DbContext> GetAllObjectSources()
        {
            //return this.GetOpenSessions() as IDictionary<string, TObjectSource>;
            var sessions = this.GetOpenSessions();//<TObjectSource>();

            return sessions;
        }


        /// <summary>
        /// Aquires the DbContext from the list of open sessions dictionary which are stored in the <see cref="IState"/> of the application. 
        /// </summary>
        /// <typeparam name="TObjectSource"></typeparam>
        /// <param name="name">Name of the session stored in the dictionary</param>
        /// <returns>Loosely typed <see cref="DbContext"/>. It needs to be loosely typed as <see cref="Object"/> since the base 
        /// <see cref="ObjectSourceLifetimeManager"/> has the flexibility to return any type of "object data" sources such as EF6, NHibernate, or any other ORM data source.
        /// In this case, it is a concrete implementation for EFCore3.x</returns>
        public override DbContext GetObjectSource(Type type)
        {
            DbContext local;
            if (!this.OpenSessions.Keys.Contains(type.AssemblyQualifiedName))
            {
                local = this.AddContextSession(type);
            }
            else
            {
                Debug.WriteLine(base.GetType().ToString() + ": Using existing Context - '" + type.AssemblyQualifiedName + "'");
                local = this.OpenSessions[type.AssemblyQualifiedName];
            }
            return local;
        }

        public override void RegisterObjectSource(Func<DbContext> objectSource)
        {
            if (!this.OpenSessions.ContainsKey(objectSource().GetType().AssemblyQualifiedName))
            {
                Debug.WriteLine(base.GetType().ToString() + ": Creating new Context - '" + objectSource().GetType().AssemblyQualifiedName + "'");
                //TObjectSource instance = ServiceLocatorWorker.GetInstance<TObjectSource>(name);
                this.OpenSessions.Add(new KeyValuePair<string, DbContext>(objectSource().GetType().AssemblyQualifiedName, objectSource()));
                lock (_lock)
                {
                    _stateStorage.Local.Remove<IDictionary<string, DbContext>>("ObjectContextSessions");
                    _stateStorage.Local.Put<IDictionary<string, DbContext>>("ObjectContextSessions", this.OpenSessions);
                }
            }
        }

        public IDictionary<string, DbContext> OpenSessions
        {
            get
            {
                if (ReferenceEquals(this._openSessions, null))
                {
                    var sessions = this.GetOpenSessions();
                    this._openSessions = new Dictionary<string, DbContext>(); // Start with a new dictionary

                    // Add any existing sessions to the Dictionary
                    sessions.ForEach(x => this._openSessions.Add(x.Key, x.Value));
                    //this._openSessions = sessions as IDictionary<string, DbContext>;

                }
                return this._openSessions;
            }
        }

    }
}

