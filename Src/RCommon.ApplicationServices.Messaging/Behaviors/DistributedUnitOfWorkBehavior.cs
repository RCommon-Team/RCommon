using MediatR;
using Microsoft.Extensions.Logging;
using RCommon.DataServices;
using RCommon.DataServices.Transactions;
using RCommon.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices.Messaging.Behaviors
{
    public class DistributedUnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> 
        where TRequest : notnull
    {
        private readonly ILogger<DistributedUnitOfWorkBehavior<TRequest, TResponse>> _logger;
        private readonly IDistributedEventBroker _distributedEventBroker;
        private readonly IUnitOfWorkScopeFactory _unitOfWorkScopeFactory;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public DistributedUnitOfWorkBehavior(IUnitOfWorkScopeFactory unitOfWorkScopeFactory, IUnitOfWorkManager unitOfWorkManager,
            ILogger<DistributedUnitOfWorkBehavior<TRequest, TResponse>> logger, IDistributedEventBroker distributedEventBroker)
        {
            _unitOfWorkScopeFactory = unitOfWorkScopeFactory ?? throw new ArgumentException(nameof(IUnitOfWorkScopeFactory));
            _unitOfWorkManager = unitOfWorkManager  ?? throw new ArgumentException(nameof(IUnitOfWorkManager)); 
            _logger = logger ?? throw new ArgumentException(nameof(ILogger));
            _distributedEventBroker = distributedEventBroker;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            var response = default(TResponse);
            var typeName = request.GetGenericTypeName();

            try
            {
                if (this._unitOfWorkManager.CurrentUnitOfWork == null)
                {
                    return await next();
                }

                using (var unitOfWork = this._unitOfWorkScopeFactory.Create(TransactionMode.Default))
                {
                    _logger.LogInformation("----- Begin transaction {UnitOfWorkTransactionId} for {CommandName} ({@Command})", 
                        this._unitOfWorkManager.CurrentUnitOfWork.TransactionId, typeName, request);

                    response = await next();

                    _logger.LogInformation("----- Commit transaction {UnitOfWorkTransactionId} for {CommandName}", 
                        this._unitOfWorkManager.CurrentUnitOfWork.TransactionId, typeName);

                    unitOfWork.Commit();
                }

                //Perform MassTransit publish events
                await _distributedEventBroker.PublishDistributedEvents(cancellationToken);


                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR Handling transaction for {CommandName} ({@Command})", typeName, request);

                throw;
            }
        }
    }
}
