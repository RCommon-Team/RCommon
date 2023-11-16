
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
