using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PROJECT2106.Data;

namespace PROJECT2106.Tests.Infrastructure;

[Collection(PostgresCollection.Name)]
public abstract class IntegrationTestBase :
    IAsyncLifetime
{
    protected readonly PostgresFixture Postgres;

    protected ProjectWebApplicationFactory Factory = null!;

    protected IntegrationTestBase(
        PostgresFixture postgres)
    {
        Postgres = postgres;
    }

    public virtual async Task InitializeAsync()
    {
        Factory =
            new ProjectWebApplicationFactory(
                Postgres.ConnectionString);

        using var scope =
            Factory.Services.CreateScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        await ResetDatabaseAsync(db);
    }

    public virtual Task DisposeAsync()
    {
        Factory.Dispose();

        return Task.CompletedTask;
    }

    protected async Task WithDbAsync(
        Func<AppDbContext, Task> action)
    {
        using var scope =
            Factory.Services.CreateScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        await action(db);
    }

    protected async Task<T> WithDbAsync<T>(
        Func<AppDbContext, Task<T>> action)
    {
        using var scope =
            Factory.Services.CreateScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        return await action(db);
    }

    private static async Task ResetDatabaseAsync(
        AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            TRUNCATE TABLE
                "Notifications",
                "PlaceRatings",
                "PostMedia",
                "Likes",
                "Comments",
                "PostTags",
                "Posts",
                "Follows",
                "Places",
                "CustomCategories",
                "AspNetUserRoles",
                "AspNetUsers",
                "AspNetRoles"
            RESTART IDENTITY CASCADE;
            """);
    }
}
