using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECT2106.Data;
using PROJECT2106.Models;
using PROJECT2106.ViewModels;

namespace PROJECT2106.Controllers;

public class MapController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public MapController(
        AppDbContext db,
        UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    [Authorize]
    public IActionResult My()
    {
        ViewBag.MyMap = true;

        return View("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Markers(bool mine = false)
    {
        var query = _db.Places
            .AsNoTracking()
            .AsQueryable();

        if (mine)
        {
            if (User.Identity?.IsAuthenticated != true)
                return Unauthorized();

            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId == null)
                return Unauthorized();

            query = query.Where(p =>
                p.CreatedByUserId == currentUserId ||
                p.Posts.Any(post =>
                    post.AuthorId == currentUserId));
        }

        var previewImageUrls = await _db.PostMedia
            .AsNoTracking()
            .Where(m =>
                m.MediaType == PostMediaType.Image &&
                m.Post != null &&
                m.Post.PlaceId != null)
            .GroupBy(m => m.Post!.PlaceId!.Value)
            .Select(group => new
            {
                PlaceId = group.Key,
                PreviewImageUrl = group
                    .OrderByDescending(m => m.Post!.CreatedAt)
                    .ThenBy(m => m.SortOrder)
                    .ThenBy(m => m.Id)
                    .Select(m => m.RelativePath)
                    .FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.PlaceId, x => x.PreviewImageUrl);

        var places = await query
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Latitude,
                p.Longitude,
                p.SystemCategory,
                CustomCategoryName = p.CustomCategory != null
                    ? p.CustomCategory.Name
                    : null,
                CustomCategoryIconPath = p.CustomCategory != null
                    ? p.CustomCategory.IconPath
                    : null,
                Rating = p.Ratings
                    .Select(r => (double?)r.Value)
                    .Average()
            })
            .ToListAsync();

        var markers = places
            .Select(p => new PlaceMarkerViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Latitude = p.Latitude,
                Longitude = p.Longitude,

                Category = p.SystemCategory.HasValue
                    ? p.SystemCategory.Value.ToString()
                    : p.CustomCategoryName ?? "Other",

                IsCustomCategory = !p.SystemCategory.HasValue,

                IconPath = p.CustomCategoryIconPath,

                PreviewImageUrl = previewImageUrls.TryGetValue(
                    p.Id,
                    out var previewImageUrl)
                    ? previewImageUrl
                    : null,

                Rating = p.Rating
            })
            .ToList();

        return Json(markers);
    }

    [HttpGet]
    public async Task<IActionResult> Nearby(
        double latitude,
        double longitude)
    {
        if (latitude < -90 || latitude > 90 ||
            longitude < -180 || longitude > 180)
        {
            return BadRequest();
        }

        const double radiusMeters = 50.0;

        // Approximate bounding box used only to reduce DB candidates.
        const double metersPerDegreeLatitude = 111_320.0;

        var latitudeDelta =
            radiusMeters / metersPerDegreeLatitude;

        var longitudeScale =
            Math.Cos(latitude * Math.PI / 180.0);

        var longitudeDelta =
            Math.Abs(longitudeScale) < 0.000001
                ? 180.0
                : radiusMeters /
                (metersPerDegreeLatitude * longitudeScale);

        var candidates = await _db.Places
            .AsNoTracking()
            .Where(p =>
                p.Latitude >= latitude - latitudeDelta &&
                p.Latitude <= latitude + latitudeDelta &&
                p.Longitude >= longitude - longitudeDelta &&
                p.Longitude <= longitude + longitudeDelta)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Latitude,
                p.Longitude
            })
            .ToListAsync();

        var nearby = candidates
            .Select(p => new NearbyPlaceViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                DistanceMeters = CalculateDistanceMeters(
                    latitude,
                    longitude,
                    p.Latitude,
                    p.Longitude)
            })
            .Where(p => p.DistanceMeters <= radiusMeters)
            .OrderBy(p => p.DistanceMeters)
            .ToList();

        return Json(nearby);
    }

    private static double CalculateDistanceMeters(
        double latitude1,
        double longitude1,
        double latitude2,
        double longitude2)
    {
        const double earthRadiusMeters = 6_371_000.0;

        var latitude1Radians =
            latitude1 * Math.PI / 180.0;

        var latitude2Radians =
            latitude2 * Math.PI / 180.0;

        var latitudeDeltaRadians =
            (latitude2 - latitude1) * Math.PI / 180.0;

        var longitudeDeltaRadians =
            (longitude2 - longitude1) * Math.PI / 180.0;

        var a =
            Math.Sin(latitudeDeltaRadians / 2) *
            Math.Sin(latitudeDeltaRadians / 2) +
            Math.Cos(latitude1Radians) *
            Math.Cos(latitude2Radians) *
            Math.Sin(longitudeDeltaRadians / 2) *
            Math.Sin(longitudeDeltaRadians / 2);

        var c =
            2 * Math.Atan2(
                Math.Sqrt(a),
                Math.Sqrt(1 - a));

        return earthRadiusMeters * c;
    }
}
