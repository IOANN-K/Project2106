using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PROJECT2106.Controllers;
using PROJECT2106.Data;
using PROJECT2106.Models;
using PROJECT2106.Tests.Infrastructure;
using PROJECT2106.ViewModels;

namespace PROJECT2106.Tests.Posts;

public sealed class PostBusinessTests
    : IntegrationTestBase
{
    public PostBusinessTests(
        PostgresFixture postgres)
        : base(postgres)
    {
    }

    [Fact]
    public async Task Author_Can_Edit_Own_Post()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var user = await TestData.CreateUserAsync(
            services,
            "post-owner");

        var db = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        var place = CreatePlace(user);

        var post = new Post
        {
            Content = "Old content",
            AuthorId = user.Id,
            Place = place,
            CreatedAt = DateTime.Now
        };

        db.Places.Add(place);
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            userManager,
            user);

        var result = await controller.Edit(
            post.Id,
            new PostEditViewModel
            {
                Id = post.Id,
                Content = "Updated content"
            });

        Assert.IsType<RedirectToActionResult>(result);

        var saved = await db.Posts
            .AsNoTracking()
            .SingleAsync(p => p.Id == post.Id);

        Assert.Equal("Updated content", saved.Content);
        Assert.True(saved.IsEdited);
        Assert.NotNull(saved.EditedAt);
    }

    [Fact]
    public async Task Foreign_User_Cannot_Edit_Post()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var owner = await TestData.CreateUserAsync(
            services,
            "post-edit-owner");

        var foreignUser = await TestData.CreateUserAsync(
            services,
            "post-edit-foreign");

        var db = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        var place = CreatePlace(owner);

        var post = new Post
        {
            Content = "Protected content",
            AuthorId = owner.Id,
            Place = place,
            CreatedAt = DateTime.Now
        };

        db.Places.Add(place);
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            userManager,
            foreignUser);

        var result = await controller.Edit(
            post.Id,
            new PostEditViewModel
            {
                Id = post.Id,
                Content = "Hacked"
            });

        Assert.IsType<ForbidResult>(result);

        var saved = await db.Posts
            .AsNoTracking()
            .SingleAsync(p => p.Id == post.Id);

        Assert.Equal("Protected content", saved.Content);
    }

    [Fact]
    public async Task Foreign_Authenticated_User_Can_Contribute_To_Another_Users_Place()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var owner = await TestData.CreateUserAsync(
            services,
            "place-post-owner");

        var contributor = await TestData.CreateUserAsync(
            services,
            "place-post-contributor");

        var db = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        var place = CreatePlace(owner);

        db.Places.Add(place);
        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            userManager,
            contributor);

        var result = await controller.Create(
            new PostCreateViewModel
            {
                Content = "Foreign contribution",
                PlaceId = place.Id
            });

        Assert.IsType<RedirectToActionResult>(result);

        var saved = await db.Posts
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(contributor.Id, saved.AuthorId);
        Assert.Equal(place.Id, saved.PlaceId);
        Assert.Equal("Foreign contribution", saved.Content);
    }

    [Fact]
    public async Task Author_Can_Delete_Own_Post()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var owner = await TestData.CreateUserAsync(
            services,
            "post-delete-owner");

        var db = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        var place = CreatePlace(owner);

        var post = new Post
        {
            Content = "Delete me",
            AuthorId = owner.Id,
            Place = place,
            CreatedAt = DateTime.Now
        };

        db.Places.Add(place);
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            userManager,
            owner);

        var result = await controller.DeleteConfirmed(post.Id);

        Assert.IsType<RedirectToActionResult>(result);

        Assert.False(
            await db.Posts.AnyAsync(
                p => p.Id == post.Id));
    }

    [Fact]
    public async Task Foreign_User_Cannot_Delete_Post()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var owner = await TestData.CreateUserAsync(
            services,
            "post-delete-owner2");

        var foreignUser = await TestData.CreateUserAsync(
            services,
            "post-delete-foreign");

        var db = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        var place = CreatePlace(owner);

        var post = new Post
        {
            Content = "Do not delete",
            AuthorId = owner.Id,
            Place = place,
            CreatedAt = DateTime.Now
        };

        db.Places.Add(place);
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            userManager,
            foreignUser);

        var result = await controller.DeleteConfirmed(post.Id);

        Assert.IsType<ForbidResult>(result);

        Assert.True(
            await db.Posts.AnyAsync(
                p => p.Id == post.Id));
    }

    [Fact]
    public async Task Invalid_Media_Extension_Is_Rejected()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var user = await TestData.CreateUserAsync(
            services,
            "media-user");

        var db = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        var place = CreatePlace(user);
        db.Places.Add(place);
        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            userManager,
            user);

        var bytes = new byte[] { 1, 2, 3, 4 };

        await using var stream = new MemoryStream(bytes);

        var file = new FormFile(
            stream,
            0,
            bytes.Length,
            "MediaFiles",
            "payload.exe")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream"
        };

        var result = await controller.Create(
            new PostCreateViewModel
            {
                Content = "Bad media",
                PlaceId = place.Id,
                MediaFiles = new List<IFormFile>
                {
                    file
                }
            });

        Assert.IsType<ViewResult>(result);

        Assert.False(controller.ModelState.IsValid);

        Assert.False(
            await db.Posts.AnyAsync());
    }

    [Fact]
    public async Task More_Than_Ten_Media_Files_Is_Rejected()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var user = await TestData.CreateUserAsync(
            services,
            "media-limit-user");

        var db = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        var place = CreatePlace(user);
        db.Places.Add(place);
        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            userManager,
            user);

        var files = Enumerable.Range(1, 11)
            .Select(i =>
            {
                var bytes = new byte[] { 1, 2, 3 };

                return (IFormFile)new FormFile(
                    new MemoryStream(bytes),
                    0,
                    bytes.Length,
                    "MediaFiles",
                    $"image-{i}.jpg")
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "image/jpeg"
                };
            })
            .ToList();

        var result = await controller.Create(
            new PostCreateViewModel
            {
                Content = "Too many files",
                PlaceId = place.Id,
                MediaFiles = files
            });

        Assert.IsType<ViewResult>(result);

        Assert.False(controller.ModelState.IsValid);

        Assert.False(
            await db.Posts.AnyAsync());
    }

    private static Place CreatePlace(AppUser owner)
    {
        return new Place
        {
            Name = $"Place-{Guid.NewGuid():N}",
            Latitude = 48.4647,
            Longitude = 35.0462,
            CreatedByUserId = owner.Id,
            CreatedAt = DateTime.Now,
            SystemCategory = SystemCategory.Other
        };
    }

    private static PostController CreateController(
        AppDbContext db,
        UserManager<AppUser> userManager,
        AppUser user)
    {
        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id),

            new Claim(
                ClaimTypes.Name,
                user.UserName!)
        };

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                claims,
                "Test"));

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        return new PostController(
            db,
            NullLogger<PostController>.Instance,
            userManager)
        {
            ControllerContext =
                new ControllerContext
                {
                    HttpContext = httpContext
                }
        };
    }
}
