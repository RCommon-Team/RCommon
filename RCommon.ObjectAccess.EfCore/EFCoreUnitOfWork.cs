#region license
//Copyright 2010 Ritesh Rao 

//Licensed under the Apache License, Version 2.0 (the "License"); 
//you may not use this file except in compliance with the License. 
//You may obtain a copy of the License at 

//http://www.apache.org/licenses/LICENSE-2.0 

//Unless required by applicable law or agreed to in writing, software 
//distributed under the License is distributed on an "AS IS" BASIS, 
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
//See the License for the specific language governing permissions and 
//limitations under the License. 
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.EntityFrameworkCore;
using RCommon.DataServices.Transactions;
using RCommon.Domain.Repositories;
using RCommon.Extensions;

namespace RCommon.ObjectAccess.EFCore
{
    /// <summary>
    /// Implements the <see cref="IUnitOfWork"/> interface to provide an implementation
    /// of a IUnitOfWork that uses Entity Framework to query and update the underlying store.
    /// </summary>
    public sealed class EFCoreUnitOfWork : DisposableResource, IUnitOfWork
    {
        private bool _disposed;
        EFObjectSourceLifetimeManager _objectSourceLifetimeManager;

        public EFCoreUnitOfWork(EFObjectSourceLifetimeManager objectSourceLifetimeManager)
        {
            _objectSourceLifetimeManager = objectSourceLifetimeManager;
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

        public void Flush()
        {
            Guard.Against<ObjectDisposedException>(this._disposed, "The current EFUnitOfWork instance has been disposed. Cannot get sessions from a disposed UnitOfWork instance.");
            var dbs = _objectSourceLifetimeManager.GetAllObjectSources();

            dbs.ForEach<KeyValuePair<string, DbContext>>(m => m.Value.SaveChanges());
        }
    }
}