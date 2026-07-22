using Xunit;

namespace RCommon.IntegrationTests.Fixtures;

[CollectionDefinition(Name)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSql";
}
