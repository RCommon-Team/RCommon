using System.Threading;
using System.Threading.Tasks;
using RCommon.Models.Commands;
using RCommon.Models.ExecutionResults;
using RCommon.Persistence.Transactions;

namespace RCommon.ApplicationServices.Commands
{
    /// <summary>
    /// Decorates <see cref="ICommandBus"/> so every <c>DispatchCommandAsync</c> call is wrapped in an
    /// <see cref="IUnitOfWork"/>, committed automatically after a successful dispatch -- the native-bus
    /// equivalent of RCommon.Mediatr's <c>AddUnitOfWorkToRequestPipeline()</c>. Registered only when
    /// <c>AddUnitOfWorkToCommandBus()</c> is called; scoped to commands only, since queries are
    /// read-only by CQRS convention.
    /// </summary>
    public class UnitOfWorkCommandBus : ICommandBus
    {
        private readonly ICommandBus _inner;
        private readonly IUnitOfWorkFactory _unitOfWorkFactory;

        /// <summary>
        /// Initializes a new instance of <see cref="UnitOfWorkCommandBus"/>.
        /// </summary>
        /// <param name="inner">The decorated <see cref="ICommandBus"/> that actually dispatches to handlers.</param>
        /// <param name="unitOfWorkFactory">Factory used to create the <see cref="IUnitOfWork"/> wrapping each dispatch.</param>
        public UnitOfWorkCommandBus(ICommandBus inner, IUnitOfWorkFactory unitOfWorkFactory)
        {
            _inner = inner;
            _unitOfWorkFactory = unitOfWorkFactory;
        }

        /// <inheritdoc />
        public async Task<TResult> DispatchCommandAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
            where TResult : IExecutionResult
        {
            using (var unitOfWork = _unitOfWorkFactory.Create(TransactionMode.Default))
            {
                var result = await _inner.DispatchCommandAsync(command, cancellationToken).ConfigureAwait(false);
                await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }
        }
    }
}
