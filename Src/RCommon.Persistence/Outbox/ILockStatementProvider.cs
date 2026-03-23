namespace RCommon.Persistence.Outbox;

public interface ILockStatementProvider
{
    string ProviderName { get; }
}
