using Microsoft.Extensions.DependencyInjection;
using RCommon.StateStorage;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private readonly IDataStoreProvider _dataStoreProvider;
        private readonly IServiceProvider _serviceProvider;

        public UnitOfWork(IDataStoreProvider dataStoreProvider, IServiceProvider serviceProvider)
        {
            this._dataStoreProvider = dataStoreProvider;
            _serviceProvider = serviceProvider;
        }

        public async Task FlushAsync()
        {
            Guard.Against<ObjectDisposedException>(this._disposed, "The current UnitOfWork instance has been disposed. Cannot get registered IDataStores from a disposed UnitOfWork instance.");
            var registeredTypes = this._dataStoreProvider.GetRegisteredDataStores(x => x.TransactionId == this.TransactionId);

            foreach (var item in registeredTypes)
            {
                await item.DataStore.PersistChangesAsync();
                //await item.DataStore.DisposeAsync(); // This should be managed through the lifetime of the DI container.
            }

           await _dataStoreProvider.RemoveRegisterdDataStoresAsync(this.TransactionId.Value);
        }


        public Nullable<Guid> TransactionId { get; set; }

        

    }
}