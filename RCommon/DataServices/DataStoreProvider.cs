using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RCommon.DataServices.Transactions;
using RCommon.StateStorage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Transactions;

namespace RCommon.DataServices
{
    public class DataStoreProvider : DisposableResource, IDataStoreProvider
    {
        private ICollection<StoredDataSource> _registeredDataStores = new List<StoredDataSource>();
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public DataStoreProvider(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            this._serviceProvider = serviceProvider;
            this._configuration = configuration;
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

        public TDataStore GetDataStore<TDataStore>(string dataStoreName = null)
            where TDataStore : IDataStore
        {
            TDataStore dataStore;
            if (!string.IsNullOrEmpty(dataStoreName))
            {
                Debug.WriteLine("Looking for DataStoreType with name: " + dataStoreName + " and type: " + typeof(TDataStore).AssemblyQualifiedName);
                // Start with finding out if there is a configuration file
                var dataStoreConfig = new DataStoreConfiguration();
                _configuration.GetSection("RCommonDataStoreTypes").Bind(dataStoreConfig);

                // If there is a configuration file, then enumerate all of the DataStoreTypes there
                if (dataStoreConfig != null)
                {
                    foreach (var dataStoreType in dataStoreConfig.DataStoreTypes)
                    {
                        if (dataStoreType.Name == dataStoreName)
                        {
                            // We found the type we are looking for, so instantiate it
                            var type = Type.GetType(dataStoreType.TypeName);
                            return (TDataStore)this._serviceProvider.GetService(type);
                        }
                    }
                }
                else
                {
                    throw new UnsupportedDataStoreException("RCommon was not able to deserialize the appsettings.json file (IConfiguration) object to create the"
                        + " DataStoreTypes required. See: http://reactor2.com/rcommon/configuration to determine the appropriate json structure.");
                }
            }

            // If no named resource then it is assumed we are trying to use a default
            dataStore = this._serviceProvider.GetService<TDataStore>();

            Guard.Against<UnsupportedDataStoreException>(dataStore == null, "The IDataStore of type: " + typeof(TDataStore).AssemblyQualifiedName + " was not registered with the dependency injection container."
                + " You can manually handle the dependency registration i.e. IServiceCollection.AddTransient<RCommonDbContext, MyDbContext>(); or RCommon allows you to"
                + " handle the registration through the fluent configuration interface. See: http://reactor2.com/rcommon/configuration");

            return dataStore;
        }

        public TDataStore GetDataStore<TDataStore>()
            where TDataStore : IDataStore
        {
            return this.GetDataStore<TDataStore>(null);
        }

        public TDataStore GetDataStore<TDataStore>(Guid transactionId)
            where TDataStore : IDataStore
        {
            return this.GetDataStore<TDataStore>(transactionId, null);

        }

        public TDataStore GetDataStore<TDataStore>(Guid transactionId, string dataStoreName = null)
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
                TDataStore newDataStore;
                if (string.IsNullOrEmpty(dataStoreName))
                {
                    // No DataStore Name is used, so use the default that is registed with the application. Note that if multiple types
                    // are registered, then we use the first one, which may cause issues. Should we throw exception instead?
                    newDataStore = this.GetDataStore<TDataStore>();
                }
                else
                {
                    // Use the named DataStoreType. This is the least likely to cause issues unless they are configuration related issues.
                    this.GetDataStore<TDataStore>(dataStoreName);
                }
                    newDataStore = this._serviceProvider.GetService<TDataStore>();
                Guard.Against<UnsupportedDataStoreException>(newDataStore == null, "The IDataStore of type: " + typeof(TDataStore).AssemblyQualifiedName + " was not registered with the dependency injection container."
                + " You can manually handle the dependency registration i.e. IServiceCollection.AddTransient<RCommonDbContext, MyDbContext>(); or RCommon allows you to"
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
