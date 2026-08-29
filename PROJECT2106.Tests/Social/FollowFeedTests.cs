using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PROJECT2106.Controllers;
using PROJECT2106.Data;
using PROJECT2106.Models;
using PROJECT2106.Tests.Infrastructure;
using PROJECT2106.ViewModels;

namespace PROJECT2106.Tests.Social;

public sealed class FollowFeedTests
    : IntegrationTestBase
{
    public FollowFeedTests(
        PostgresFixture postgres)
        : base(postgres)
    {
    }

    [Fact]
    public async Task Follow_Creates_Relationship()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var follower = await TestData.CreateUserAsync(
            services,
            "feed-follower");

        var followed = await TestData.CreateUserAsync(
            services,
            "feed-followed");

        var db =
            services.GetRequiredService<AppDbContext>();

        var userManager =
            services.GetRequiredService<UserManager<AppUser>>();

        var controller = CreateFollowController(
            db,
            userManager,
            follower);

        var result =
            await controller.Follow(
                followed.UserName!);

        Assert.IsType<RedirectToActionResult>(result);

        Assert.True(
            await db.Follows.AnyAsync(f =>
                f.FollowerId == follower.Id &&
                f.FollowingId == followed.Id));
    }

    [Fact]
    public async Task Followed_Users_Post_Appears_In_Feed()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var follower = await TestData.CreateUserAsync(
            services,
            "feed-reader");

        var followed = await TestData.CreateUserAsync(
            services,
            "feed-author");

        var unrelated = await TestData.CreateUserAsync(
            services,
            "feed-unrelated");

        var db =
            services.GetRequiredService<AppDbContext>();

        var userManager =
            services.GetRequiredService<UserManager<AppUser>>();

        var followedPlace =
            CreatePlace(followed, "Followed place");

        var unrelatedPlace =
            CreatePlace(unrelated, "Unrelated place");

        db.Places.AddRange(
            followedPlace,
            unrelatedPlace);

        await db.SaveChangesAsync();

        var followedPost = new Post
        {
            Content = "Visible in feed",
            AuthorId = followed.Id,
            PlaceId = followedPlace.Id,
            CreatedAt = DateTime.Now
        };

        var unrelatedPost = new Post
        {
            Content = "Must not be visible",
            AuthorId = unrelated.Id,
            PlaceId = unrelatedPlace.Id,
            CreatedAt = DateTime.Now
        };

        db.Posts.AddRange(
            followedPost,
            unrelatedPost);

        db.Follows.Add(new Follow
        {
            FollowerId = follower.Id,
            FollowingId = followed.Id,
            CreatedAt = DateTime.Now
        });

        await db.SaveChangesAsync();

        var controller = CreatePostController(
            db,
            userManager,
            follower);

        var result =
            await controller.Feed();

        var view =
            Assert.IsType<ViewResult>(result);

        var feed =
            Assert.IsType<
                PagedResult<FeedItemViewModel>>(
                view.Model);

        var items = feed.Items.ToList();

        Assert.Single(items);

        Assert.Equal(
            followedPost.Id,
            items[0].PostId);

        Assert.Equal(
            followed.UserName,
            items[0].AuthorUsername);
    }

    [Fact]
    public async Task Unfollow_Removes_User_From_Feed()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var follower = await TestData.CreateUserAsync(
            services,
            "unfollow-reader");

        var followed = await TestData.CreateUserAsync(
            services,
            "unfollow-author");

        var db =
            services.GetRequiredService<AppDbContext>();

        var userManager =
            services.GetRequiredService<UserManager<AppUser>>();

        var place =
            CreatePlace(
                followed,
                "Unfollow place");

        db.Places.Add(place);
        await db.SaveChangesAsync();

        db.Posts.Add(new Post
        {
            Content = "Initially visible",
            AuthorId = followed.Id,
            PlaceId = place.Id,
            CreatedAt = DateTime.Now
        });

        db.Follows.Add(new Follow
        {
            FollowerId = follower.Id,
            FollowingId = followed.Id,
            CreatedAt = DateTime.Now
        });

        await db.SaveChangesAsync();

        var followController =
            CreateFollowController(
                db,
                userManager,
                follower);

        var unfollowResult =
            await followController.Unfollow(
                followed.UserName!);

        Assert.IsType<
            RedirectToActionResult>(
            unfollowResult);

        Assert.False(
            await db.Follows.AnyAsync(f =>
                f.FollowerId == follower.Id &&
                f.FollowingId == followed.Id));

        var postController =
            CreatePostController(
                db,
                userManager,
                follower);

        var feedResult =
            await postController.Feed();

        var view =
            Assert.IsType<ViewResult>(
                feedResult);

        var feed =
            Assert.IsType<
                PagedResult<FeedItemViewModel>>(
                view.Model);

        Assert.Empty(feed.Items);
    }

    [Fact]
    public async Task User_Cannot_Follow_Themself()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var user = await TestData.CreateUserAsync(
            services,
            "self-follow");

        var db =
            services.GetRequiredService<AppDbContext>();

        var userManager =
            services.GetRequiredService<UserManager<AppUser>>();

        var controller =
            CreateFollowController(
                db,
                userManager,
                user);

        var result =
            await controller.Follow(
                user.UserName!);

        Assert.IsType<BadRequestResult>(result);

        Assert.False(
            await db.Follows.AnyAsync());
    }

    [Fact]
    public async Task Duplicate_Follow_Is_Rejected()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var follower = await TestData.CreateUserAsync(
            services,
            "duplicate-follower");

        var followed = await TestData.CreateUserAsync(
            services,
            "duplicate-followed");

        var db =
            services.GetRequiredService<AppDbContext>();

        var userManager =
            services.GetRequiredService<UserManager<AppUser>>();

        db.Follows.Add(new Follow
        {
            FollowerId = follower.Id,
            FollowingId = followed.Id,
            CreatedAt = DateTime.Now
        });

        await db.SaveChangesAsync();

        var controller =
            CreateFollowController(
                db,
                userManager,
                follower);

        var result =
            await controller.Follow(
                followed.UserName!);

        Assert.IsType<BadRequestResult>(result);

        Assert.Equal(
            1,
            await db.Follows.CountAsync());
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

    private static FollowController CreateFollowController(
        AppDbContext db,
        UserManager<AppUser> userManager,
        AppUser user)
    {
        return new FollowController(
            db,
            userManager)
        {
            ControllerContext =
                CreateControllerContext(user)
        };
    }

    private static PostController CreatePostController(
        AppDbContext db,
        UserManager<AppUser> userManager,
        AppUser user)
    {
        return new PostController(
            db,
            NullLogger<PostController>.Instance,
            userManager)
        {
            ControllerContext =
                CreateControllerContext(user)
        };
    }

    private static ControllerContext CreateControllerContext(
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

        return new ControllerContext
        {
            HttpContext =
                new DefaultHttpContext
                {
                    User = principal
                }
        };
    }
}
