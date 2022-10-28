

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RCommon.DataServices.Transactions
{

    public class UnitOfWorkManager : IUnitOfWorkManager
    {
        private ILogger<UnitOfWorkManager> _logger;
        private readonly IServiceProvider _serviceProvider;


        public UnitOfWorkManager(IStateStorage stateStorage, ILogger<UnitOfWorkManager> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
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
