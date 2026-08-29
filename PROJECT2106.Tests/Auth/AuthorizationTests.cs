using Microsoft.Extensions.DependencyInjection;
using System.Net;
using PROJECT2106.Tests.Infrastructure;

namespace PROJECT2106.Tests.Auth;

public sealed class AuthorizationTests
    : IntegrationTestBase
{
    public AuthorizationTests(
        PostgresFixture postgres)
        : base(postgres)
    {
    }

    [Fact]
    public async Task Anonymous_Cannot_Open_Place_Create()
    {
        var client =
            Factory.CreateClient(
                new()
                {
                    AllowAutoRedirect = false
                });

        var response =
            await client.GetAsync("/Place/Create");

        Assert.True(
            response.StatusCode is
                HttpStatusCode.Redirect or
                HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Anonymous_Cannot_Open_Post_Create()
    {
        var client =
            Factory.CreateClient(
                new()
                {
                    AllowAutoRedirect = false
                });

        var response =
            await client.GetAsync("/Post/Create?placeId=1");

        Assert.True(
            response.StatusCode is
                HttpStatusCode.Redirect or
                HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Anonymous_Cannot_Edit_Profile()
    {
        var client = Factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Profile/Edit");

        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Anonymous_Cannot_Change_Password()
    {
        var client = Factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Account/ChangePassword");

        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Normal_User_Cannot_Open_Admin()
    {
        using var scope =
            Factory.Services.CreateScope();

        var user =
            await TestData.CreateUserAsync(
                scope.ServiceProvider,
                "regular-user");

        var client =
            TestData.CreateAuthenticatedClient(
                Factory,
                user);

        var response =
            await client.GetAsync("/Admin");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
}
