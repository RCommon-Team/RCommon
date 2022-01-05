

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RCommon.DependencyInjection;
using RCommon.StateStorage;

namespace RCommon.DataServices.Transactions
{

    public class UnitOfWorkManager : IUnitOfWorkManager
    {
        private IStateStorage _stateStorage;
        private IUnitOfWorkTransactionManager _currentTransactionManager;
        private ILogger<UnitOfWorkManager> _logger;
        private readonly IServiceProvider _serviceProvider;
        private const string LocalTransactionManagerKey = "UnitOfWorkManager.LocalTransactionManager";


        public UnitOfWorkManager(IStateStorage stateStorage, IUnitOfWorkTransactionManager transactionManager, ILogger<UnitOfWorkManager> logger,
            IServiceProvider serviceProvider)
        {
            _stateStorage = stateStorage;

            _logger = logger;
            _serviceProvider = serviceProvider;
            this.SetTransactionManagerProvider(() => transactionManager);
        }




        ///<summary>
        /// Sets a <see cref="Func{T}"/> of <see cref="IUnitOfWorkTransactionManager"/> that the 
        /// <see cref="UnitOfWorkManager"/> uses to get an instance of <see cref="IUnitOfWorkTransactionManager"/>
        ///</summary>
        ///<param name="provider"></param>
        public void SetTransactionManagerProvider(Func<IUnitOfWorkTransactionManager> transactionManager)
        {
            Guard.Against<ArgumentNullException>(transactionManager == null, "transactionManager parameter cannot be null");
            _logger.LogDebug("The transaction manager provider is being set or overriden. Using supplied" +
                                " trasaction manager provider.");
            _stateStorage.LocalContext.Remove<IUnitOfWorkTransactionManager>(LocalTransactionManagerKey);
            _stateStorage.LocalContext.Put(LocalTransactionManagerKey, transactionManager);
            _currentTransactionManager = transactionManager();

        }

        /// <summary>
        /// Gets the current <see cref="IUnitOfWorkTransactionManager"/>.
        /// </summary>
        public IUnitOfWorkTransactionManager CurrentTransactionManager
        {
            get
            {

                return _currentTransactionManager;
            }
        }

        /// <summary>
        /// Gets the current <see cref="IUnitOfWork"/> instance.
        /// </summary>
        public IUnitOfWork CurrentUnitOfWork
        {
            get
            {
                return _currentTransactionManager.CurrentUnitOfWork;
            }
        }
    }
}
