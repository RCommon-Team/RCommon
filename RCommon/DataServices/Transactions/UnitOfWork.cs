using Microsoft.Extensions.DependencyInjection;
using RCommon.StateStorage;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
            Guard.Against<ObjectDisposedException>(this._disposed, "The current UnitOfWork instance has been disposed. Cannot get registered IDataStores from a disposed UnitOfWork instance.");
            var registeredTypes = this.GetAllRegisteredDataStores();

            foreach (var item in registeredTypes)
            {
                var actualType = Type.GetType(item); // item should be fully qualified assembly name per RegisterDataStoretype method
                var dataStore = this._serviceProvider.GetService(actualType) as IDataStore; // actualType should be IDataStore per RegisterDataStoretype method 
                dataStore.PersistChanges();
                dataStore.Dispose();
            }

            _storage.Local.Clear();
        }

        public async Task FlushAsync()
        {
            Guard.Against<ObjectDisposedException>(this._disposed, "The current UnitOfWork instance has been disposed. Cannot get registered IDataStores from a disposed UnitOfWork instance.");
            var registeredTypes = this.GetAllRegisteredDataStores();

            foreach (var item in registeredTypes)
            {
                var actualType = Type.GetType(item); // item should be fully qualified assembly name per RegisterDataStoretype method
                var dataStore = this._serviceProvider.GetService(actualType) as IDataStore; // actualType should be IDataStore per RegisterDataStoretype method 
                await dataStore.PersistChangesAsync();
                await dataStore.DisposeAsync();
            }

            _storage.Local.Clear();
        }

        public void RegisterDataStoreType<TDataStoreType>()
            where TDataStoreType : IDataStore
        {
            
            // Check the type to verify that it is registered is with DI container
            var type = this._serviceProvider.GetService(typeof(TDataStoreType));
            if (type == null)
            {
                throw new UnsupportedDataStoreException(typeof(TDataStoreType));
            }
            
            var registeredDataStores = _storage.Local.Get<List<string>>(this.GetType().AssemblyQualifiedName);
            if (registeredDataStores == null)
            {
                registeredDataStores = new List<string>();
            }

            var typeAssemblyQualifiedName = typeof(TDataStoreType).AssemblyQualifiedName;
            var potentialDataStore = registeredDataStores.First(x => x == typeAssemblyQualifiedName);
            
            if (potentialDataStore == null)
            {
                registeredDataStores.Add(typeAssemblyQualifiedName); // We really dont need the entire object stored 
            }
            
            _storage.Local.Put<List<string>>(this.GetType().AssemblyQualifiedName, registeredDataStores);
        }

        private List<string> GetAllRegisteredDataStores()
        {
            var registeredDataStores = _storage.Local.Get<List<string>>(this.GetType().AssemblyQualifiedName);
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