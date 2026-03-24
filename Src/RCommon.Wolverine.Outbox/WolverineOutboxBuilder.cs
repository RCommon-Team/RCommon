using Wolverine;
using Wolverine.EntityFrameworkCore;

namespace RCommon.Wolverine.Outbox;

public class WolverineOutboxBuilder : IWolverineOutboxBuilder
{
    private readonly WolverineOptions _wolverineOptions;

    public WolverineOutboxBuilder(WolverineOptions wolverineOptions)
    {
        _wolverineOptions = wolverineOptions ?? throw new ArgumentNullException(nameof(wolverineOptions));
    }

    public IWolverineOutboxBuilder UseEntityFrameworkCoreTransactions()
    {
        _wolverineOptions.UseEntityFrameworkCoreTransactions();
        return this;
    }
}
