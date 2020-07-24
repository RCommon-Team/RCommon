using Microsoft.Extensions.DependencyInjection;
using RCommon.DataServices.Transactions;
using RCommon.StateStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace RCommon.DataServices
{
    public class DataStoreProvider : DisposableResource, IDataStoreProvider
    {
        private ICollection<StoredDataSource> _registeredDataStores = new List<StoredDataSource>();
        private readonly IServiceProvider _serviceProvider;

        

        public DataStoreProvider(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }


        public void RegisterDataSource<TDataStore>(Guid transactionId, TDataStore dataStore)
            where TDataStore : IDataStore
        {
            var newType = typeof(TDataStore).AssemblyQualifiedName;
            bool bFound = false;
            foreach (var item in this._registeredDataStores)
            {
                if (item.DataStore.GetType().AssemblyQualifiedName == newType && item.TransactionId == transactionId)
                {
                    bFound = true;
                    break; // We don't need to add it to the registered data stores because it already exists
                }

            }

            if (!bFound)
            {
                this._registeredDataStores.Add(new StoredDataSource(transactionId, dataStore));
            }

        }

        public TDataStore GetDataStore<TDataStore>()
            where TDataStore : IDataStore
        {
            // No transaction Id so just give them a new transient instance of the IDataStore
            var dataStore = this._serviceProvider.GetService<TDataStore>();

            Guard.Against<UnsupportedDataStoreException>(dataStore == null, "The IDataStore of type: " + typeof(TDataStore).AssemblyQualifiedName + " was not registered with the dependency injection container."
                + " You can manually handle the dependency registration i.e. IServiceCollection.AddTransient<MyDbContext>(); or RCommon allows you to"
                + " handle the registration through the fluent configuration interface. See: http://reactor2.com/rcommon/configuration");

            return dataStore;
        }

        public TDataStore GetDataStore<TDataStore>(Guid transactionId)
            where TDataStore : IDataStore
        {
            var newType = typeof(TDataStore).AssemblyQualifiedName;
            bool bFound = false;
            foreach (var item in this._registeredDataStores)
            {
                if (item.DataStore.GetType().AssemblyQualifiedName == newType && item.TransactionId == transactionId)
                {
                    bFound = true;
                    return (TDataStore)item.DataStore; // 
                }

            }

            if (!bFound)
            {
                var newDataStore = this._serviceProvider.GetService<TDataStore>();
                Guard.Against<UnsupportedDataStoreException>(newDataStore == null, "The IDataStore of type: " + typeof(TDataStore).AssemblyQualifiedName + " was not registered with the dependency injection container."
                + " You can manually handle the dependency registration i.e. IServiceCollection.AddTransient<MyDbContext>(); or RCommon allows you to"
                + " handle the registration through the fluent configuration interface. See: http://reactor2.com/rcommon/configuration");

                this._registeredDataStores.Add(new StoredDataSource(transactionId, newDataStore));
                return newDataStore;
            }

            throw new ApplicationException("This is odd. We should have either found an existing IDataStore or created a new one. Get a developer on this!");

        }

        public IEnumerable<StoredDataSource> GetRegisteredDataStores(Func<StoredDataSource, bool> criteria)
        {
            return this._registeredDataStores.AsQueryable().Where(criteria);
        }

        public void RemoveRegisterdDataStores(Guid transactionId)
        {
            var existingDataStores = _registeredDataStores.ToList(); // create a copy
            foreach (var item in existingDataStores)
            {
                if (item.TransactionId == transactionId)
                {
                    _registeredDataStores.Remove(item);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }


    }


}
