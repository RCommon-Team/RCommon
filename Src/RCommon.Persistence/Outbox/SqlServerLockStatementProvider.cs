namespace RCommon.Persistence.Outbox;

public class SqlServerLockStatementProvider : ILockStatementProvider
{
    public string ProviderName => "SqlServer";
}
