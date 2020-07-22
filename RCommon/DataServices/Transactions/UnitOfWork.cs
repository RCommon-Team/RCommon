using Microsoft.Extensions.DependencyInjection;
using RCommon.StateStorage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.DataServices.Transactions
{
    /// <summary>
    /// Implements the <see cref="IUnitOfWork"/> interface to provide an implementation
    /// of a IUnitOfWork that supports transaction persistance no matter what the data store type is.
    /// </summary>
    public class UnitOfWork : DisposableResource, IUnitOfWork
    {
        private bool _disposed;
        private readonly IStateStorage _storage;
        private readonly IServiceProvider _serviceProvider;

        public UnitOfWork(IServiceProvider serviceProvider, IStateStorage stateStorage)
        {
            _serviceProvider = serviceProvider;
            _storage = stateStorage;
        }

        

        public void Flush()
        {
            Guard.Against<ObjectDisposedException>(this._disposed, "The current EFUnitOfWork instance has been disposed. Cannot get sessions from a disposed UnitOfWork instance.");
            var dbs = this._serviceProvider.GetServices<IDataStore>();

            foreach (var db in dbs)
            {
                db.PersistChanges();
            }

            foreach (var db in dbs) // Each datastore should automatically get disposed at the end of the lifetime scope but this gives us fine grained control
            {
                db.Dispose();
            }

            _storage.Local.Clear();
            //dbs.ForEach<KeyValuePair<string, DbContext>>(m => m.Value.SaveChanges());
        }

        public async Task FlushAsync()
        {
            Guard.Against<ObjectDisposedException>(this._disposed, "The current EFUnitOfWork instance has been disposed. Cannot get sessions from a disposed UnitOfWork instance.");
            var dbs = this._serviceProvider.GetServices<IDataStore>();

            foreach (var db in dbs)
            {
                await db.PersistChangesAsync();
            }

            foreach (var db in dbs) // Each datastore should automatically get disposed at the end of the lifetime scope but this gives us fine grained control
            {
                await db.DisposeAsync();
            }

            _storage.Local.Clear();
            //dbs.ForEach<KeyValuePair<string, DbContext>>(m => m.Value.SaveChanges());
        }

        public void RegisterDataStoreType(IDataStore dataStore)
        {
            dataStore = null; // We don't really care about the stuff inside, just the type
            var registeredDataStores = _storage.Local.Get<IDictionary<string, IDataStore>>(this.GetType().AssemblyQualifiedName);
            if (registeredDataStores == null)
            {
                registeredDataStores = new Dictionary<string, IDataStore>();
            }
            
            var potentialDataStore = registeredDataStores.First(x => x.Key == dataStore.GetType().AssemblyQualifiedName).Value;
            if (potentialDataStore == null)
            {
                registeredDataStores.Add(dataStore.GetType().AssemblyQualifiedName, dataStore); // We really dont need the entire object stored 
            }
            
            _storage.Local.Put<IDictionary<string, IDataStore>>(this.GetType().AssemblyQualifiedName, registeredDataStores);
        }

        private IDictionary<string, IDataStore> GetAllRegisteredDataStores()
        {
            var registeredDataStores = _storage.Local.Get<IDictionary<string, IDataStore>>(this.GetType().AssemblyQualifiedName);
            return registeredDataStores;
        }

        protected override void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {

                }
                this._disposed = true;
            }
        }

        protected async override Task DisposeAsync(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {

                }
                this._disposed = true;
            }
            await Task.CompletedTask;
        }
    }
}