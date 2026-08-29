namespace PROJECT2106.Tests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class PostgresCollection :
    ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
