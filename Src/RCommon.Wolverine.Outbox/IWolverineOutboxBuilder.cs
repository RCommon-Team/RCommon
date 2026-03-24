namespace RCommon.Wolverine.Outbox;

public interface IWolverineOutboxBuilder
{
    IWolverineOutboxBuilder UseEntityFrameworkCoreTransactions();
}
