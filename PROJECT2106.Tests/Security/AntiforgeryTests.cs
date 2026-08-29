using System.Net;
using Microsoft.Extensions.DependencyInjection;
using PROJECT2106.Tests.Infrastructure;

namespace PROJECT2106.Tests.Security;

public sealed class AntiforgeryTests
    : IntegrationTestBase
{
    public AntiforgeryTests(
        PostgresFixture postgres)
        : base(postgres)
    {
    }

    [Fact]
    public async Task Authenticated_Follow_Post_Without_Antiforgery_Token_Is_Rejected()
    {
        using var scope =
            Factory.Services.CreateScope();

        var currentUser =
            await TestData.CreateUserAsync(
                scope.ServiceProvider,
                "csrf-follower");

        var targetUser =
            await TestData.CreateUserAsync(
                scope.ServiceProvider,
                "csrf-target");

        var client =
            TestData.CreateAuthenticatedClient(
                Factory,
                currentUser);

        var response =
            await client.PostAsync(
                $"/Follow/Follow?username={Uri.EscapeDataString(targetUser.UserName!)}",
                content: null);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_Logout_Without_Antiforgery_Token_Is_Rejected()
    {
        using var scope =
            Factory.Services.CreateScope();

        var user =
            await TestData.CreateUserAsync(
                scope.ServiceProvider,
                "csrf-logout-user");

        var client =
            TestData.CreateAuthenticatedClient(
                Factory,
                user);

        var response =
            await client.PostAsync(
                "/Account/Logout",
                content: null);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_Like_Post_Without_Antiforgery_Token_Is_Rejected()
    {
        using var scope =
            Factory.Services.CreateScope();

        var user =
            await TestData.CreateUserAsync(
                scope.ServiceProvider,
                "csrf-like-user");

        var client =
            TestData.CreateAuthenticatedClient(
                Factory,
                user);

        var response =
            await client.PostAsync(
                "/Like/ToggleLike?postId=999999&isLike=true",
                content: null);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
}
