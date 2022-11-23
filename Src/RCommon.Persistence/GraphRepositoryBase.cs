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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon.BusinessEntities;
using RCommon.Collections;
using RCommon.DataServices;
using RCommon.DataServices.Transactions;
using RCommon.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    ///<summary>
    /// A base class for implementors of <see cref="IRepository{TEntity}"/>.
    ///</summary>
    ///<typeparam name="TEntity"></typeparam>
    public abstract class GraphRepositoryBase<TEntity> : LinqRepositoryBase<TEntity>, IGraphRepository<TEntity>
        where TEntity : class, IBusinessEntity
    {

        private string _dataStoreName;
        private readonly IDataStoreEnlistmentProvider _dataStoreEnlistmentProvider;

        public GraphRepositoryBase(IDataStoreRegistry dataStoreRegistry, IDataStoreEnlistmentProvider dataStoreEnlistmentProvider,
            IUnitOfWorkManager unitOfWorkManager, IEventTracker eventTracker, IOptions<DefaultDataStoreOptions> defaultDataStoreOptions)
            :base(dataStoreRegistry, dataStoreEnlistmentProvider, unitOfWorkManager, eventTracker, defaultDataStoreOptions)
        {
            
        }

        public abstract bool Tracking { get; set; }
    }
}
