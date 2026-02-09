using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace RCommon.Persistence.Transactions
{
    /// <summary>
    /// Default implementation of <see cref="IUnitOfWorkFactory"/> that creates <see cref="IUnitOfWork"/>
    /// instances from the DI container with optional transaction mode and isolation level overrides.
    /// </summary>
    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IGuidGenerator _guidGenerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWorkFactory"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider used to resolve <see cref="IUnitOfWork"/> instances.</param>
        /// <param name="guidGenerator">The GUID generator (passed through to resolved unit of work instances).</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        public UnitOfWorkFactory(IServiceProvider serviceProvider, IGuidGenerator guidGenerator)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _guidGenerator = guidGenerator;
        }

        /// <inheritdoc />
        public IUnitOfWork Create()
        {
            var unitOfWork = _serviceProvider.GetService<IUnitOfWork>();
            return unitOfWork!;
        }

        /// <inheritdoc />
        public IUnitOfWork Create(TransactionMode transactionMode)
        {
            var unitOfWork = _serviceProvider.GetRequiredService<IUnitOfWork>();
            unitOfWork.TransactionMode = transactionMode;
            return unitOfWork;
        }

        /// <inheritdoc />
        public IUnitOfWork Create(TransactionMode transactionMode, IsolationLevel isolationLevel)
        {
            var unitOfWork = _serviceProvider.GetRequiredService<IUnitOfWork>();
            unitOfWork.TransactionMode = transactionMode;
            return unitOfWork;
        }
    }
}
