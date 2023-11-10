using MediatR;
using Microsoft.Extensions.Logging;
using RCommon.DataServices;
using RCommon.DataServices.Transactions;
using RCommon.Extensions;
using RCommon.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Mediator.MediatR.Behaviors
{
    public class DistributedUnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<DistributedUnitOfWorkBehavior<TRequest, TResponse>> _logger;
        private readonly IDistributedEventPublisher _distributedEventBroker;
        private readonly IUnitOfWorkFactory _unitOfWorkScopeFactory;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public DistributedUnitOfWorkBehavior(IUnitOfWorkFactory unitOfWorkScopeFactory, IUnitOfWorkManager unitOfWorkManager,
            ILogger<DistributedUnitOfWorkBehavior<TRequest, TResponse>> logger, IDistributedEventPublisher distributedEventBroker)
        {
            _unitOfWorkScopeFactory = unitOfWorkScopeFactory ?? throw new ArgumentException(nameof(IUnitOfWorkFactory));
            _unitOfWorkManager = unitOfWorkManager  ?? throw new ArgumentException(nameof(IUnitOfWorkManager)); 
            _logger = logger ?? throw new ArgumentException(nameof(ILogger));
            _distributedEventBroker = distributedEventBroker ?? throw new ArgumentNullException(nameof(distributedEventBroker));
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var response = default(TResponse);
            var typeName = request.GetGenericTypeName();

            try
            {
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
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "ERROR Handling transaction for {CommandName} ({@Command})", typeName, request);

                throw;
            }
        }
    }
}
