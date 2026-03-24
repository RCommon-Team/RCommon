namespace RCommon.Persistence.Outbox;

public class PostgreSqlLockStatementProvider : ILockStatementProvider
{
    public string ProviderName => "PostgreSql";
}
