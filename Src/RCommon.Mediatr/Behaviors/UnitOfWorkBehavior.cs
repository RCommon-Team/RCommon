using MediatR;
using Microsoft.Extensions.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;
using RCommon.Persistence.Transactions;

namespace RCommon.Mediator.MediatR.Behaviors
{
    public class UnitOfWorkRequestBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest
    {
        private readonly ILogger<UnitOfWorkRequestBehavior<TRequest, TResponse>> _logger;
        private readonly IUnitOfWorkFactory _unitOfWorkScopeFactory;

        public UnitOfWorkRequestBehavior(IUnitOfWorkFactory unitOfWorkScopeFactory,
            ILogger<UnitOfWorkRequestBehavior<TRequest, TResponse>> logger)
        {
            _unitOfWorkScopeFactory = unitOfWorkScopeFactory ?? throw new ArgumentException(nameof(IUnitOfWorkFactory));
            _logger = logger ?? throw new ArgumentException(nameof(ILogger));
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
                        unitOfWork.TransactionId, typeName, request);

                    response = await next();

                    _logger.LogInformation("----- Commit transaction {UnitOfWorkTransactionId} for {CommandName}",
                        unitOfWork.TransactionId, typeName);

                    unitOfWork.Commit();
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR Handling transaction for {CommandName} ({@Command})", typeName, request);

                throw;
            }
        }
    }

    public class UnitOfWorkRequestWithResponseBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<UnitOfWorkRequestWithResponseBehavior<TRequest, TResponse>> _logger;
        private readonly IUnitOfWorkFactory _unitOfWorkScopeFactory;

        public UnitOfWorkRequestWithResponseBehavior(IUnitOfWorkFactory unitOfWorkScopeFactory,
            ILogger<UnitOfWorkRequestWithResponseBehavior<TRequest, TResponse>> logger)
        {
            _unitOfWorkScopeFactory = unitOfWorkScopeFactory ?? throw new ArgumentException(nameof(IUnitOfWorkFactory));
            _logger = logger ?? throw new ArgumentException(nameof(ILogger));
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
                        unitOfWork.TransactionId, typeName, request);

                    response = await next();

                    _logger.LogInformation("----- Commit transaction {UnitOfWorkTransactionId} for {CommandName}",
                        unitOfWork.TransactionId, typeName);

                    unitOfWork.Commit();
                }

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
