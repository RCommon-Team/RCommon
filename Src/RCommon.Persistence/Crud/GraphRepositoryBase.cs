
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon.Entities;
using RCommon.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using RCommon.Persistence.Transactions;

namespace RCommon.Persistence.Crud
{
    ///<summary>
    /// A base class for implementors of <see cref="IGraphRepository{TEntity}"/>.
    ///</summary>
    ///<typeparam name="TEntity"></typeparam>
    public abstract class GraphRepositoryBase<TEntity> : LinqRepositoryBase<TEntity>, IGraphRepository<TEntity>
        where TEntity : class, IBusinessEntity
    {

        private string _dataStoreName;
        private readonly IDataStoreEnlistmentProvider _dataStoreEnlistmentProvider;

        public GraphRepositoryBase(IDataStoreRegistry dataStoreRegistry, IDataStoreEnlistmentProvider dataStoreEnlistmentProvider,
            IUnitOfWorkManager unitOfWorkManager, IEntityEventTracker eventTracker, IOptions<DefaultDataStoreOptions> defaultDataStoreOptions)
            :base(dataStoreRegistry, dataStoreEnlistmentProvider, unitOfWorkManager, eventTracker, defaultDataStoreOptions)
        {
            
        }

        public abstract bool Tracking { get; set; }
    }
}
