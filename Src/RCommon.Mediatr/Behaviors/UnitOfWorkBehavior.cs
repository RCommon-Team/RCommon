using MediatR;
using Microsoft.Extensions.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;
using RCommon.Persistence.Transactions;

namespace RCommon.Mediator.MediatR.Behaviors
{
    /// <summary>
    /// MediatR pipeline behavior that wraps fire-and-forget request handling in a transactional unit of work.
    /// Creates a unit of work before the handler executes and commits it upon successful completion.
    /// </summary>
    /// <typeparam name="TRequest">The MediatR request type. Must implement <see cref="IRequest"/>.</typeparam>
    /// <typeparam name="TResponse">The response type from the pipeline.</typeparam>
    public class UnitOfWorkRequestBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest
    {
        private readonly ILogger<UnitOfWorkRequestBehavior<TRequest, TResponse>> _logger;
        private readonly IUnitOfWorkFactory _unitOfWorkScopeFactory;

        /// <summary>
        /// Initializes a new instance of <see cref="UnitOfWorkRequestBehavior{TRequest, TResponse}"/>.
        /// </summary>
        /// <param name="unitOfWorkScopeFactory">Factory for creating unit of work instances.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        public UnitOfWorkRequestBehavior(IUnitOfWorkFactory unitOfWorkScopeFactory,
            ILogger<UnitOfWorkRequestBehavior<TRequest, TResponse>> logger)
        {
            _unitOfWorkScopeFactory = unitOfWorkScopeFactory ?? throw new ArgumentException(nameof(IUnitOfWorkFactory));
            _logger = logger ?? throw new ArgumentException(nameof(ILogger));
        }

        /// <inheritdoc />
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var response = default(TResponse);
            var typeName = request.GetGenericTypeName();

            try
            {
                // Create a unit of work with default transaction mode; disposed automatically on scope exit
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

    /// <summary>
    /// MediatR pipeline behavior that wraps request-with-response handling in a transactional unit of work.
    /// Creates a unit of work before the handler executes and commits it upon successful completion.
    /// </summary>
    /// <typeparam name="TRequest">The MediatR request type. Must implement <see cref="IRequest{TResponse}"/>.</typeparam>
    /// <typeparam name="TResponse">The response type returned by the handler.</typeparam>
    public class UnitOfWorkRequestWithResponseBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<UnitOfWorkRequestWithResponseBehavior<TRequest, TResponse>> _logger;
        private readonly IUnitOfWorkFactory _unitOfWorkScopeFactory;

        /// <summary>
        /// Initializes a new instance of <see cref="UnitOfWorkRequestWithResponseBehavior{TRequest, TResponse}"/>.
        /// </summary>
        /// <param name="unitOfWorkScopeFactory">Factory for creating unit of work instances.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        public UnitOfWorkRequestWithResponseBehavior(IUnitOfWorkFactory unitOfWorkScopeFactory,
            ILogger<UnitOfWorkRequestWithResponseBehavior<TRequest, TResponse>> logger)
        {
            _unitOfWorkScopeFactory = unitOfWorkScopeFactory ?? throw new ArgumentException(nameof(IUnitOfWorkFactory));
            _logger = logger ?? throw new ArgumentException(nameof(ILogger));
        }

        /// <inheritdoc />
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var response = default(TResponse);
            var typeName = request.GetGenericTypeName();

            try
            {
                // Create a unit of work with default transaction mode; disposed automatically on scope exit
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
