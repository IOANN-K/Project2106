using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using PROJECT2106.Controllers;
using PROJECT2106.Data;
using PROJECT2106.Models;
using PROJECT2106.Tests.Infrastructure;
using PROJECT2106.ViewModels;

namespace PROJECT2106.Tests.Map;

public sealed class MyMapTests
    : IntegrationTestBase
{
    public MyMapTests(
        PostgresFixture postgres)
        : base(postgres)
    {
    }

    [Fact]
    public async Task My_Map_Returns_Owned_And_Contributed_Places_Only_Once()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var currentUser = await TestData.CreateUserAsync(
            services,
            "my-map-user");

        var otherUser = await TestData.CreateUserAsync(
            services,
            "my-map-other");

        var db =
            services.GetRequiredService<AppDbContext>();

        var userManager =
            services.GetRequiredService<UserManager<AppUser>>();

        var ownedPlace = CreatePlace(
            currentUser,
            "Owned Place",
            48.4647,
            35.0462);

        var contributedPlace = CreatePlace(
            otherUser,
            "Contributed Place",
            48.4650,
            35.0470);

        var unrelatedPlace = CreatePlace(
            otherUser,
            "Unrelated Place",
            48.4700,
            35.0500);

        db.Places.AddRange(
            ownedPlace,
            contributedPlace,
            unrelatedPlace);

        await db.SaveChangesAsync();

        // Two contributions to the same Place.
        // The marker must still appear only once.
        db.Posts.AddRange(
            new Post
            {
                Content = "Contribution one",
                AuthorId = currentUser.Id,
                PlaceId = contributedPlace.Id,
                CreatedAt = DateTime.Now
            },
            new Post
            {
                Content = "Contribution two",
                AuthorId = currentUser.Id,
                PlaceId = contributedPlace.Id,
                CreatedAt = DateTime.Now
            });

        // Another user's post must not make the unrelated
        // Place appear on currentUser's map.
        db.Posts.Add(
            new Post
            {
                Content = "Other user's post",
                AuthorId = otherUser.Id,
                PlaceId = unrelatedPlace.Id,
                CreatedAt = DateTime.Now
            });

        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            userManager,
            currentUser);

        var result =
            await controller.Markers(mine: true);

        var json =
            Assert.IsType<JsonResult>(result);

        var markers =
            Assert.IsAssignableFrom<
                IEnumerable<PlaceMarkerViewModel>>(
                json.Value);

        var list = markers.ToList();

        Assert.Equal(2, list.Count);

        Assert.Contains(
            list,
            marker => marker.Id == ownedPlace.Id);

        Assert.Contains(
            list,
            marker => marker.Id == contributedPlace.Id);

        Assert.DoesNotContain(
            list,
            marker => marker.Id == unrelatedPlace.Id);

        Assert.Equal(
            1,
            list.Count(
                marker =>
                    marker.Id == contributedPlace.Id));
    }

    [Fact]
    public async Task Mine_Markers_Without_Authentication_Returns_Unauthorized()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var db =
            services.GetRequiredService<AppDbContext>();

        var userManager =
            services.GetRequiredService<UserManager<AppUser>>();

        var controller =
            new MapController(
                db,
                userManager)
            {
                ControllerContext =
                    new ControllerContext
                    {
                        HttpContext =
                            new DefaultHttpContext()
                    }
            };

        var result =
            await controller.Markers(mine: true);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Anonymous_User_Cannot_Open_My_Map_Page()
    {
        var client =
            Factory.CreateClient(
                new()
                {
                    AllowAutoRedirect = false
                });

        var response =
            await client.GetAsync("/Map/My");

        Assert.True(
            response.StatusCode
                is HttpStatusCode.Redirect
                or HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Global_Map_Returns_All_Places()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var user1 = await TestData.CreateUserAsync(
            services,
            "global-map-user-1");

        var user2 = await TestData.CreateUserAsync(
            services,
            "global-map-user-2");

        var db =
            services.GetRequiredService<AppDbContext>();

        var userManager =
            services.GetRequiredService<UserManager<AppUser>>();

        db.Places.AddRange(
            CreatePlace(
                user1,
                "Global Place 1",
                48.4647,
                35.0462),

            CreatePlace(
                user2,
                "Global Place 2",
                48.4700,
                35.0500));

        await db.SaveChangesAsync();

        var controller =
            new MapController(
                db,
                userManager)
            {
                ControllerContext =
                    new ControllerContext
                    {
                        HttpContext =
                            new DefaultHttpContext()
                    }
            };

        var result =
            await controller.Markers();

        var json =
            Assert.IsType<JsonResult>(result);

        var markers =
            Assert.IsAssignableFrom<
                IEnumerable<PlaceMarkerViewModel>>(
                json.Value);

        Assert.Equal(
            2,
            markers.Count());
    }

    private static Place CreatePlace(
        AppUser owner,
        string name,
        double latitude,
        double longitude)
    {
        return new Place
        {
            Name = name,
            Latitude = latitude,
            Longitude = longitude,
            CreatedByUserId = owner.Id,
            CreatedAt = DateTime.Now,
            SystemCategory = SystemCategory.Other
        };
    }

    private static MapController CreateController(
        AppDbContext db,
        UserManager<AppUser> userManager,
        AppUser user)
    {
        var principal =
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(
                            ClaimTypes.NameIdentifier,
                            user.Id),

                        new Claim(
                            ClaimTypes.Name,
                            user.UserName!)
                    },
                    "Test"));

        return new MapController(
            db,
            userManager)
        {
            ControllerContext =
                new ControllerContext
                {
                    HttpContext =
                        new DefaultHttpContext
                        {
                            User = principal
                        }
                }
        };
    }
}
