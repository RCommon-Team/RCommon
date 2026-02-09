
/* Unmerged change from project 'RCommon.Persistence (net8.0)'
Before:
using System;
After:
using System;
using RCommon;
using RCommon.Persistence;
using RCommon.Persistence;
using RCommon.Persistence.Transactions;
*/
using System;

namespace RCommon.Persistence.Transactions
{
    /// <summary>
    /// Builder interface for configuring unit of work services and default settings during application startup.
    /// </summary>
    /// <seealso cref="DefaultUnitOfWorkBuilder"/>
    /// <seealso cref="UnitOfWorkSettings"/>
    public interface IUnitOfWorkBuilder
    {
        /// <summary>
        /// Configures the default <see cref="UnitOfWorkSettings"/> such as isolation level and auto-complete behavior.
        /// </summary>
        /// <param name="unitOfWorkOptions">An action to configure the <see cref="UnitOfWorkSettings"/>.</param>
        /// <returns>The current <see cref="IUnitOfWorkBuilder"/> instance for fluent chaining.</returns>
        IUnitOfWorkBuilder SetOptions(Action<UnitOfWorkSettings> unitOfWorkOptions);
    }
}