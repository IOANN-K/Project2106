using Microsoft.AspNetCore.Http;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PROJECT2106.Controllers;
using PROJECT2106.Data;
using PROJECT2106.Models;
using PROJECT2106.Tests.Infrastructure;
using PROJECT2106.ViewModels;

namespace PROJECT2106.Tests.Places;

public sealed class PlaceBusinessTests
    : IntegrationTestBase
{
    public PlaceBusinessTests(
        PostgresFixture postgres)
        : base(postgres)
    {
    }

    [Fact]
    public async Task Authenticated_User_Can_Create_Valid_Place()
    {
        using var scope =
            Factory.Services.CreateScope();

        var services = scope.ServiceProvider;

        var user =
            await TestData.CreateUserAsync(
                services,
                "place-owner");

        var db =
            services.GetRequiredService<AppDbContext>();

        var userManager =
            services.GetRequiredService<UserManager<AppUser>>();

        var controller =
            CreateController(
                db,
                userManager,
                user);

        var model = new PlaceCreateViewModel
        {
            Name = "Test Lake",
            Latitude = 48.4647,
            Longitude = 35.0462,
            Description = "Integration test place",
            SystemCategory = SystemCategory.Nature
        };

        var result =
            await controller.Create(model);

        var redirect =
            Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal(
            nameof(PlaceController.Details),
            redirect.ActionName);

        var place =
            await db.Places
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal("Test Lake", place.Name);
        Assert.Equal(48.4647, place.Latitude);
        Assert.Equal(35.0462, place.Longitude);
        Assert.Equal(user.Id, place.CreatedByUserId);
        Assert.Equal(
            SystemCategory.Nature,
            place.SystemCategory);
    }

    [Theory]
    [InlineData(-91, 35)]
    [InlineData(91, 35)]
    [InlineData(48, -181)]
    [InlineData(48, 181)]
    public async Task Database_Rejects_Invalid_Coordinates(
        double latitude,
        double longitude)
    {
        using var scope =
            Factory.Services.CreateScope();

        var services = scope.ServiceProvider;

        var user =
            await TestData.CreateUserAsync(
                services,
                $"coords-{Guid.NewGuid():N}");

        var db =
            services.GetRequiredService<AppDbContext>();

        db.Places.Add(new Place
        {
            Name = "Invalid coordinates",
            Latitude = latitude,
            Longitude = longitude,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.Now,
            SystemCategory = SystemCategory.Other
        });

        await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Place_Details_Uses_Newest_Image_From_Contributions_As_Preview()
    {
        using var scope =
            Factory.Services.CreateScope();

        var services = scope.ServiceProvider;

        var user =
            await TestData.CreateUserAsync(
                services,
                "preview-owner");

        var db =
            services.GetRequiredService<AppDbContext>();

        var userManager =
            services.GetRequiredService<UserManager<AppUser>>();

        var place = new Place
        {
            Name = "Preview Pick Place",
            Latitude = 48.4647,
            Longitude = 35.0462,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            SystemCategory = SystemCategory.Nature
        };

        var olderPost = new Post
        {
            Content = "Older note",
            AuthorId = user.Id,
            Place = place,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var newerPost = new Post
        {
            Content = "Newer note",
            AuthorId = user.Id,
            Place = place,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        db.Places.Add(place);
        db.Posts.AddRange(olderPost, newerPost);
        db.PostMedia.AddRange(
            new PostMedia
            {
                Post = olderPost,
                MediaType = PostMediaType.Image,
                OriginalFileName = "older.png",
                StoredFileName = "older.png",
                RelativePath = "/uploads/posts/older.png",
                MimeType = "image/png",
                SizeBytes = 12,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new PostMedia
            {
                Post = newerPost,
                MediaType = PostMediaType.Video,
                OriginalFileName = "newer.mp4",
                StoredFileName = "newer.mp4",
                RelativePath = "/uploads/posts/newer.mp4",
                MimeType = "video/mp4",
                SizeBytes = 18,
                SortOrder = 0,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new PostMedia
            {
                Post = newerPost,
                MediaType = PostMediaType.Image,
                OriginalFileName = "newer-image.jpg",
                StoredFileName = "newer-image.jpg",
                RelativePath = "/uploads/posts/newer-image.jpg",
                MimeType = "image/jpeg",
                SizeBytes = 20,
                SortOrder = 0,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });

        await db.SaveChangesAsync();

        var controller =
            new PlaceController(db, userManager);

        var result =
            await controller.Details(place.Id);

        var view =
            Assert.IsType<ViewResult>(result);

        var model =
            Assert.IsType<PlaceDetailsViewModel>(view.Model);

        Assert.Equal(
            "/uploads/posts/newer-image.jpg",
            model.PreviewImageUrl);
    }

    [Fact]
    public async Task Place_Details_With_No_Image_Contribution_Leaves_Preview_Empty()
    {
        using var scope =
            Factory.Services.CreateScope();

        var services = scope.ServiceProvider;

        var user =
            await TestData.CreateUserAsync(
                services,
                "video-only-owner");

        var db =
            services.GetRequiredService<AppDbContext>();

        var userManager =
            services.GetRequiredService<UserManager<AppUser>>();

        var place = new Place
        {
            Name = "Video Only Place",
            Latitude = 48.4647,
            Longitude = 35.0462,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            SystemCategory = SystemCategory.Other
        };

        var post = new Post
        {
            Content = "Only video",
            AuthorId = user.Id,
            Place = place,
            CreatedAt = DateTime.UtcNow
        };

        db.Places.Add(place);
        db.Posts.Add(post);
        db.PostMedia.Add(new PostMedia
        {
            Post = post,
            MediaType = PostMediaType.Video,
            OriginalFileName = "clip.mp4",
            StoredFileName = "clip.mp4",
            RelativePath = "/uploads/posts/clip.mp4",
            MimeType = "video/mp4",
            SizeBytes = 42,
            SortOrder = 0,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var result =
            await new PlaceController(db, userManager)
                .Details(place.Id);

        var view =
            Assert.IsType<ViewResult>(result);

        var model =
            Assert.IsType<PlaceDetailsViewModel>(view.Model);

        Assert.Null(model.PreviewImageUrl);
    }

    [Fact]
    public async Task Map_Markers_Include_Preview_Image_Url()
    {
        using var scope =
            Factory.Services.CreateScope();

        var services = scope.ServiceProvider;

        var user =
            await TestData.CreateUserAsync(
                services,
                "marker-preview-owner");

        var db =
            services.GetRequiredService<AppDbContext>();

        var place = new Place
        {
            Name = "Marker Preview Place",
            Latitude = 48.4647,
            Longitude = 35.0462,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            SystemCategory = SystemCategory.Viewpoint
        };

        var post = new Post
        {
            Content = "Marker note",
            AuthorId = user.Id,
            Place = place,
            CreatedAt = DateTime.UtcNow
        };

        db.Places.Add(place);
        db.Posts.Add(post);
        db.PostMedia.Add(new PostMedia
        {
            Post = post,
            MediaType = PostMediaType.Image,
            OriginalFileName = "marker-preview.png",
            StoredFileName = "marker-preview.png",
            RelativePath = "/uploads/posts/marker-preview.png",
            MimeType = "image/png",
            SizeBytes = 64,
            SortOrder = 3,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var result =
            await new MapController(
                db,
                services.GetRequiredService<UserManager<AppUser>>())
                .Markers();

        var json =
            Assert.IsType<JsonResult>(result);

        var markers =
            Assert.IsAssignableFrom<IEnumerable<PlaceMarkerViewModel>>(json.Value);

        var marker = Assert.Single(markers);

        Assert.Equal(
            "/uploads/posts/marker-preview.png",
            marker.PreviewImageUrl);
    }

    [Fact]
    public async Task Nearby_Returns_Place_Within_50_Meters()
    {
        using var scope =
            Factory.Services.CreateScope();

        var services = scope.ServiceProvider;

        var user =
            await TestData.CreateUserAsync(
                services,
                "nearby-owner");

        var db =
            services.GetRequiredService<AppDbContext>();

        var originLatitude = 48.4647;
        var originLongitude = 35.0462;

        db.Places.Add(new Place
        {
            Name = "Nearby Test Place",
            Latitude = 48.4648,
            Longitude = 35.0462,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.Now,
            SystemCategory = SystemCategory.Viewpoint
        });

        await db.SaveChangesAsync();

        var userManager =
            services.GetRequiredService<UserManager<AppUser>>();

        var controller =
            new MapController(
                db,
                userManager);

        var result =
            await controller.Nearby(
                originLatitude,
                originLongitude);

        var json =
            Assert.IsType<JsonResult>(result);

        var places =
            Assert.IsAssignableFrom<
                IEnumerable<NearbyPlaceViewModel>>(
                json.Value);

        var nearby =
            places.Single();

        Assert.Equal(
            "Nearby Test Place",
            nearby.Name);

        Assert.InRange(
            nearby.DistanceMeters,
            0,
            50);
    }

    [Fact]
    public async Task Nearby_Does_Not_Return_Place_Outside_50_Meters()
    {
        using var scope =
            Factory.Services.CreateScope();

        var services = scope.ServiceProvider;

        var user =
            await TestData.CreateUserAsync(
                services,
                "far-owner");

        var db =
            services.GetRequiredService<AppDbContext>();

        db.Places.Add(new Place
        {
            Name = "Far Test Place",
            Latitude = 48.4700,
            Longitude = 35.0462,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.Now,
            SystemCategory = SystemCategory.Viewpoint
        });

        await db.SaveChangesAsync();

        var userManager =
            services.GetRequiredService<UserManager<AppUser>>();

        var controller =
            new MapController(
                db,
                userManager);

        var result =
            await controller.Nearby(
                48.4647,
                35.0462);

        var json =
            Assert.IsType<JsonResult>(result);

        var places =
            Assert.IsAssignableFrom<
                IEnumerable<NearbyPlaceViewModel>>(
                json.Value);

        Assert.Empty(places);
    }

    [Fact]
    public async Task Nearby_Rejects_Invalid_Coordinates()
    {
        using var scope =
            Factory.Services.CreateScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<UserManager<AppUser>>();

        var controller =
            new MapController(
                db,
                userManager);

        var result =
            await controller.Nearby(
                91,
                35);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Foreign_User_Cannot_Delete_Place()
    {
        using var scope =
            Factory.Services.CreateScope();

        var services = scope.ServiceProvider;

        var owner =
            await TestData.CreateUserAsync(
                services,
                "delete-owner");

        var foreignUser =
            await TestData.CreateUserAsync(
                services,
                "delete-foreign");

        var db =
            services.GetRequiredService<AppDbContext>();

        var place = new Place
        {
            Name = "Protected Place",
            Latitude = 48.4647,
            Longitude = 35.0462,
            CreatedByUserId = owner.Id,
            CreatedAt = DateTime.Now,
            SystemCategory = SystemCategory.Landmark
        };

        db.Places.Add(place);
        await db.SaveChangesAsync();

        var userManager =
            services.GetRequiredService<UserManager<AppUser>>();

        var controller =
            CreateController(
                db,
                userManager,
                foreignUser);

        var result =
            await controller.DeleteConfirmed(place.Id);

        Assert.IsType<ForbidResult>(result);

        Assert.True(
            await db.Places.AnyAsync(
                p => p.Id == place.Id));
    }

    [Fact]
    public async Task Owner_Can_Delete_Place_With_Only_Own_Content()
    {
        using var scope =
            Factory.Services.CreateScope();

        var services = scope.ServiceProvider;

        var owner =
            await TestData.CreateUserAsync(
                services,
                "clean-delete-owner");

        var db =
            services.GetRequiredService<AppDbContext>();

        var place = new Place
        {
            Name = "Deletable Place",
            Latitude = 48.4647,
            Longitude = 35.0462,
            CreatedByUserId = owner.Id,
            CreatedAt = DateTime.Now,
            SystemCategory = SystemCategory.Nature
        };

        var post = new Post
        {
            Content = "Owner contribution",
            AuthorId = owner.Id,
            Place = place,
            CreatedAt = DateTime.Now
        };

        db.Places.Add(place);
        db.Posts.Add(post);

        await db.SaveChangesAsync();

        var placeId = place.Id;

        var userManager =
            services.GetRequiredService<UserManager<AppUser>>();

        var controller =
            CreateController(
                db,
                userManager,
                owner);

        var result =
            await controller.DeleteConfirmed(placeId);

        var redirect =
            Assert.IsType<RedirectToActionResult>(
                result);

        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);

        Assert.False(
            await db.Places.AnyAsync(
                p => p.Id == placeId));

        Assert.False(
            await db.Posts.AnyAsync(
                p => p.PlaceId == placeId));
    }

    [Fact]
    public async Task Owner_Cannot_Delete_Place_With_Foreign_Contribution()
    {
        using var scope =
            Factory.Services.CreateScope();

        var services = scope.ServiceProvider;

        var owner =
            await TestData.CreateUserAsync(
                services,
                "shared-place-owner");

        var contributor =
            await TestData.CreateUserAsync(
                services,
                "shared-place-contributor");

        var db =
            services.GetRequiredService<AppDbContext>();

        var place = new Place
        {
            Name = "Shared Place",
            Latitude = 48.4647,
            Longitude = 35.0462,
            CreatedByUserId = owner.Id,
            CreatedAt = DateTime.Now,
            SystemCategory = SystemCategory.PhotoSpot
        };

        db.Places.Add(place);

        db.Posts.Add(new Post
        {
            Content = "Foreign contribution",
            AuthorId = contributor.Id,
            Place = place,
            CreatedAt = DateTime.Now
        });

        await db.SaveChangesAsync();

        var userManager =
            services.GetRequiredService<UserManager<AppUser>>();

        var controller =
            CreateController(
                db,
                userManager,
                owner);

        var result =
            await controller.DeleteConfirmed(place.Id);

        Assert.IsType<ForbidResult>(result);

        Assert.True(
            await db.Places.AnyAsync(
                p => p.Id == place.Id));

        Assert.True(
            await db.Posts.AnyAsync(
                p => p.PlaceId == place.Id));
    }

    private static PlaceController CreateController(
        AppDbContext db,
        UserManager<AppUser> userManager,
        AppUser user,
        string? role = null)
    {
        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                user.Id),

            new(
                ClaimTypes.Name,
                user.UserName!)
        };

        if (!string.IsNullOrWhiteSpace(role))
        {
            claims.Add(
                new Claim(
                    ClaimTypes.Role,
                    role));
        }

        var principal =
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    claims,
                    "Test"));

        return new PlaceController(
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
