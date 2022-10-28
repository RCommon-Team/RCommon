using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using RCommon.Extensions;

namespace RCommon.DataServices.Transactions
{
    /// <summary>
    /// Implements the <see cref="IUnitOfWork"/> interface to provide an implementation
    /// of a IUnitOfWork that supports transaction persistance no matter what the data store type is.
    /// </summary>
    public class UnitOfWork : DisposableResource, IUnitOfWork
    {
        private bool _disposed;
        private readonly IDataStoreRegistry _dataStoreRegistry;
        private readonly IServiceProvider _serviceProvider;

        public UnitOfWork(IDataStoreRegistry dataStoreRegistry, IServiceProvider serviceProvider)
        {
            this._dataStoreRegistry = dataStoreRegistry;
            _serviceProvider = serviceProvider;
        }

        public void Flush(bool allowPersist)
        {
            Guard.Against<ObjectDisposedException>(this._disposed, "The current UnitOfWork instance has been disposed. Cannot get registered IDataStores from a disposed UnitOfWork instance.");
            var registeredTypes = this._dataStoreRegistry.GetRegisteredDataStores(x => x.TransactionId == this.TransactionId);
            
            foreach (var item in registeredTypes)
            {
                if (allowPersist)
                {
                    item.DataStore.PersistChanges();
                }
                //item.DataStore.Dispose(); // This should be managed through the lifetime of the DI container.
            }

            _dataStoreRegistry.RemoveRegisteredDataStores(this.TransactionId.Value);
        }


        public Nullable<Guid> TransactionId { get; set; }

        protected override Task DisposeAsync(bool disposing)
        {
            _disposed = true;
            return base.DisposeAsync(disposing);
        }

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
            base.Dispose(disposing);
        }

    }
}
