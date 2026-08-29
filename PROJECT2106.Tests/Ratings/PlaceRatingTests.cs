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

namespace PROJECT2106.Tests.Ratings;

public sealed class PlaceRatingTests
    : IntegrationTestBase
{
    public PlaceRatingTests(
        PostgresFixture postgres)
        : base(postgres)
    {
    }

    [Fact]
    public async Task User_Can_Create_Rating()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var user = await TestData.CreateUserAsync(
            services,
            "rating-user");

        var db = services.GetRequiredService<AppDbContext>();
        var userManager =
            services.GetRequiredService<UserManager<AppUser>>();

        var place = CreatePlace(user);

        db.Places.Add(place);
        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            userManager,
            user);

        var result = await controller.Rate(
            new PlaceRatingInputViewModel
            {
                PlaceId = place.Id,
                Value = 5
            });

        Assert.IsType<RedirectToActionResult>(result);

        var rating = await db.PlaceRatings
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(place.Id, rating.PlaceId);
        Assert.Equal(user.Id, rating.UserId);
        Assert.Equal(5, rating.Value);
    }

    [Fact]
    public async Task Second_Rating_By_Same_User_Updates_Existing_Row()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var user = await TestData.CreateUserAsync(
            services,
            "rating-update-user");

        var db = services.GetRequiredService<AppDbContext>();
        var userManager =
            services.GetRequiredService<UserManager<AppUser>>();

        var place = CreatePlace(user);

        db.Places.Add(place);
        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            userManager,
            user);

        await controller.Rate(
            new PlaceRatingInputViewModel
            {
                PlaceId = place.Id,
                Value = 2
            });

        await controller.Rate(
            new PlaceRatingInputViewModel
            {
                PlaceId = place.Id,
                Value = 4
            });

        var ratings = await db.PlaceRatings
            .AsNoTracking()
            .Where(r =>
                r.PlaceId == place.Id &&
                r.UserId == user.Id)
            .ToListAsync();

        Assert.Single(ratings);
        Assert.Equal(4, ratings[0].Value);
    }

    [Fact]
    public async Task Database_Rejects_Duplicate_User_Place_Rating()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var user = await TestData.CreateUserAsync(
            services,
            "rating-db-unique-user");

        var db = services.GetRequiredService<AppDbContext>();

        var place = CreatePlace(user);

        db.Places.Add(place);
        await db.SaveChangesAsync();

        db.PlaceRatings.AddRange(
            new PlaceRating
            {
                PlaceId = place.Id,
                UserId = user.Id,
                Value = 3,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new PlaceRating
            {
                PlaceId = place.Id,
                UserId = user.Id,
                Value = 4,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });

        await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task Database_Rejects_Rating_Outside_One_To_Five(
        int value)
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var user = await TestData.CreateUserAsync(
            services,
            $"rating-range-{value}");

        var db = services.GetRequiredService<AppDbContext>();

        var place = CreatePlace(user);

        db.Places.Add(place);
        await db.SaveChangesAsync();

        db.PlaceRatings.Add(new PlaceRating
        {
            PlaceId = place.Id,
            UserId = user.Id,
            Value = value,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        });

        await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Rating_Nonexistent_Place_Returns_NotFound()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var user = await TestData.CreateUserAsync(
            services,
            "rating-missing-place-user");

        var db = services.GetRequiredService<AppDbContext>();
        var userManager =
            services.GetRequiredService<UserManager<AppUser>>();

        var controller = CreateController(
            db,
            userManager,
            user);

        var result = await controller.Rate(
            new PlaceRatingInputViewModel
            {
                PlaceId = 999999,
                Value = 5
            });

        Assert.IsType<NotFoundResult>(result);
    }

    private static Place CreatePlace(AppUser owner)
    {
        return new Place
        {
            Name = $"Rating Place {Guid.NewGuid():N}",
            Latitude = 48.4647,
            Longitude = 35.0462,
            CreatedByUserId = owner.Id,
            CreatedAt = DateTime.Now,
            SystemCategory = SystemCategory.Other
        };
    }

    private static PlaceController CreateController(
        AppDbContext db,
        UserManager<AppUser> userManager,
        AppUser user)
    {
        var principal = new ClaimsPrincipal(
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
