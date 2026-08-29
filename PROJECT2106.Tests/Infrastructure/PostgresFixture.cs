using Testcontainers.PostgreSql;

namespace PROJECT2106.Tests.Infrastructure;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("project2106_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    public string ConnectionString =>
        _container.GetConnectionString();

    public Task InitializeAsync() =>
        _container.StartAsync();

    public Task DisposeAsync() =>
        _container.DisposeAsync().AsTask();
}
