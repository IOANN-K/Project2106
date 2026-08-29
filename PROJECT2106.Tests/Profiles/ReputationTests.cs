using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PROJECT2106.Controllers;
using PROJECT2106.Data;
using PROJECT2106.Models;
using PROJECT2106.Tests.Infrastructure;
using PROJECT2106.ViewModels;

namespace PROJECT2106.Tests.Profiles;

public sealed class ReputationTests
    : IntegrationTestBase
{
    public ReputationTests(
        PostgresFixture postgres)
        : base(postgres)
    {
    }

    [Fact]
    public async Task Profile_Calculates_Reputation_From_All_Contribution_Sources()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var target = await TestData.CreateUserAsync(
            services,
            "reputation-target");

        var other = await TestData.CreateUserAsync(
            services,
            "reputation-other");

        var db = services.GetRequiredService<AppDbContext>();
        var userManager =
            services.GetRequiredService<UserManager<AppUser>>();

        var place1 = CreatePlace(target, "Owned Place 1");
        var place2 = CreatePlace(target, "Owned Place 2");

        db.Places.AddRange(place1, place2);

        var post1 = new Post
        {
            Content = "Contribution 1",
            AuthorId = target.Id,
            Place = place1,
            CreatedAt = DateTime.Now
        };

        var post2 = new Post
        {
            Content = "Contribution 2",
            AuthorId = target.Id,
            Place = place2,
            CreatedAt = DateTime.Now
        };

        var post3 = new Post
        {
            Content = "Contribution 3",
            AuthorId = target.Id,
            Place = place1,
            CreatedAt = DateTime.Now
        };

        db.Posts.AddRange(
            post1,
            post2,
            post3);

        await db.SaveChangesAsync();

        db.Likes.AddRange(
            new Like
            {
                UserId = other.Id,
                PostId = post1.Id,
                IsLike = true
            },
            new Like
            {
                UserId = other.Id,
                PostId = post2.Id,
                IsLike = true
            },
            new Like
            {
                UserId = target.Id,
                PostId = post3.Id,
                IsLike = true
            });

        db.Comments.AddRange(
            new Comment
            {
                Content = "Comment 1",
                AuthorId = target.Id,
                PostId = post1.Id,
                CreatedAt = DateTime.Now
            },
            new Comment
            {
                Content = "Comment 2",
                AuthorId = target.Id,
                PostId = post2.Id,
                CreatedAt = DateTime.Now
            });

        await db.SaveChangesAsync();

        var controller =
            CreateController(
                userManager,
                db,
                target);

        var result =
            await controller.Index(
                target.UserName!,
                1);

        var view =
            Assert.IsType<ViewResult>(result);

        var model =
            Assert.IsType<ProfileViewModel>(
                view.Model);

        Assert.Equal(2, model.CreatedPlacesCount);
        Assert.Equal(3, model.ContributionsCount);

        // Self-like must not count.
        Assert.Equal(2, model.LikesReceived);

        Assert.Equal(2, model.CommentsCreated);

        // 2*10 + 3*5 + 2*2 + 2 = 41
        Assert.Equal(41, model.Reputation);

        Assert.Equal(
            "Level 1",
            model.ExplorerLevel);
    }

    [Fact]
    public async Task Reputation_Level_Two_Starts_At_Fifty()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var user = await TestData.CreateUserAsync(
            services,
            "reputation-level-two");

        var db = services.GetRequiredService<AppDbContext>();
        var userManager =
            services.GetRequiredService<UserManager<AppUser>>();

        for (var i = 0; i < 5; i++)
        {
            db.Places.Add(
                CreatePlace(
                    user,
                    $"Place {i}"));
        }

        await db.SaveChangesAsync();

        var controller =
            CreateController(
                userManager,
                db,
                user);

        var result =
            await controller.Index(
                user.UserName!,
                1);

        var view =
            Assert.IsType<ViewResult>(result);

        var model =
            Assert.IsType<ProfileViewModel>(
                view.Model);

        Assert.Equal(50, model.Reputation);
        Assert.Equal(
            "Level 2",
            model.ExplorerLevel);
    }

    private static ProfileController CreateController(
        UserManager<AppUser> userManager,
        AppDbContext db,
        AppUser currentUser)
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        currentUser.Id),

                    new Claim(
                        ClaimTypes.Name,
                        currentUser.UserName!)
                },
                "Test"));

        return new ProfileController(
            userManager,
            db)
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

    private static Place CreatePlace(
        AppUser owner,
        string name)
    {
        return new Place
        {
            Name = name,
            Latitude = 48.4647,
            Longitude = 35.0462,
            CreatedByUserId = owner.Id,
            CreatedAt = DateTime.Now,
            SystemCategory = SystemCategory.Other
        };
    }
}
