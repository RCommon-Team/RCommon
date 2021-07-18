using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<DataStoreProvider> _logger;

        public DataStoreProvider(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<DataStoreProvider> logger)
        {
            this._serviceProvider = serviceProvider;
            this._configuration = configuration;
            this._logger = logger;
        }

        public void RegisterDataStore(Guid transactionId, Type dataStore, string dataStoreName)
        {
            var newTypeName = dataStore.AssemblyQualifiedName;
            bool bFound = false;
            foreach (var item in this._registeredDataStores)
            {
                if (item.Type.AssemblyQualifiedName == newTypeName && item.DataStoreName == dataStoreName)
                {
                    bFound = true;
                    break; // We don't need to add it to the registered data stores because it already exists
                }

            }

            if (!bFound)
            {
                Debug.WriteLine("Creating New DataStore for type: " + newTypeName + " with TransactionId: " + transactionId.ToString());
                this._registeredDataStores.Add(new StoredDataSource(transactionId, dataStore, dataStoreName));
            }

        }

        public IEnumerable<StoredDataSource> GetRegisteredDataStores(Func<StoredDataSource, bool> criteria)
        {
            return this._registeredDataStores.AsQueryable().Where(criteria);
        }

        public IDataStore GetDataStore(Guid transactionId, string dataStoreName)
        {
            IDataStore dataStore = null; // Default
            string typeName = string.Empty; // Default

            // Use the named DataStoreType. This is the least likely to cause issues unless they are configuration related issues.
            Debug.WriteLine("Looking for DataStore with name: " + dataStoreName);

            if (this.CanFindTypeNameInConfig(dataStoreName, out typeName))
            {
                // Register this data source
                Type type = Type.GetType(typeName);
                dataStore = (IDataStore)this._serviceProvider.GetService(type);
                Guard.Against<UnsupportedDataStoreException>(dataStore == null, "The IDataStore of type: " + type.AssemblyQualifiedName + " was not registered with the dependency injection container."
            + " You can manually handle the dependency registration i.e. IServiceCollection.AddTransient<RCommonDbContext, MyDbContext>(); or RCommon allows you to"
            + " handle the registration through the fluent configuration interface. See: http://reactor2.com/rcommon/configuration");

                this.RegisterDataStore(transactionId, type, dataStoreName);
                return dataStore;
            }
            else
            {
                throw new UnsupportedDataStoreException("RCommon was not able to find a DataStore with the name of: " + dataStoreName + " in the configuration."
                    + " See: http://reactor2.com/rcommon/configuration to determine how to configure your DataStore with RCommon.");

            }


            throw new ApplicationException("This is odd. We should have either found an existing IDataStore or created a new one. Get a developer on this!");

        }

        public IDataStore GetDataStore(string dataStoreName)
        {
            return this.GetDataStore(Guid.Empty, dataStoreName);
        }

        private bool CanFindTypeNameInConfig(string dataStoreName, out string typeName)
        {
            // Start with finding out if there is a configuration file
            var dataStoreConfig = new DataStoreConfiguration();
            _configuration.GetSection("RCommonDataStoreTypes").Bind(dataStoreConfig);

            // If there is a configuration file, then enumerate all of the DataStoreTypes there
            if (dataStoreConfig != null)
            {
                Type type = typeof(string); // Default
                typeName = string.Empty; // Default
                foreach (var dataStoreType in dataStoreConfig.DataStoreTypes)
                {
                    if (dataStoreType.Name == dataStoreName)
                    {
                        // We found the type we are looking for, so instantiate it
                        type = Type.GetType(dataStoreType.TypeName);

                        Guard.Against<UnsupportedDataStoreException>(type == null, "We found the DataStore Name of: " + dataStoreType.Name + " but could not"
                            + " find the type associated of: " + dataStoreType.TypeName + ". Please double check the namespace, and version number.");
                        typeName = dataStoreType.TypeName;
                        return true;
                    }
                }

                return false;

            }
            else // Woops, no configuration file
            {
                throw new UnsupportedDataStoreException("RCommon was not able to deserialize the appsettings.json file (IConfiguration) object to create the"
                    + " DataStoreTypes required. See: http://reactor2.com/rcommon/configuration to determine the appropriate json structure.");
            }
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
