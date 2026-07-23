using Xunit;

namespace RCommon.IntegrationTests.Fixtures;

[CollectionDefinition(Name)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSql";
}

[CollectionDefinition(Name)]
public sealed class RabbitMqCollection : ICollectionFixture<RabbitMqFixture>
{
    public const string Name = "RabbitMq";
}

[CollectionDefinition(Name)]
public sealed class PostgresAndRabbitMqCollection
    : ICollectionFixture<PostgreSqlFixture>, ICollectionFixture<RabbitMqFixture>
{
    public const string Name = "PostgresAndRabbitMq";
}
