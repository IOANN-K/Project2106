using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECT2106.Data;
using PROJECT2106.Models;
using PROJECT2106.ViewModels;

namespace PROJECT2106.Controllers;

public class PlaceController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public PlaceController(
        AppDbContext db,
        UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Create()
    {
        var model = new PlaceCreateViewModel
        {
            CustomCategories = await GetCustomCategoriesAsync()
        };

        return View(model);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(PlaceCreateViewModel model)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Forbid();

        var hasSystem = model.SystemCategory.HasValue;
        var hasCustom = model.CustomCategoryId.HasValue;

        if (hasSystem == hasCustom)
        {
            ModelState.AddModelError(
                string.Empty,
                "Choose either a system category or a custom category.");
        }

        if (model.SystemCategory.HasValue &&
            !Enum.IsDefined(model.SystemCategory.Value))
        {
            ModelState.AddModelError(
                nameof(model.SystemCategory),
                "Invalid system category.");
        }

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "Place name is required.");
        }

        CustomCategory? customCategory = null;

        if (model.CustomCategoryId.HasValue)
        {
            customCategory = await _db.CustomCategories
                .FirstOrDefaultAsync(c =>
                    c.Id == model.CustomCategoryId.Value &&
                    c.CreatedByUserId == userId &&
                    c.IsActive);

            if (customCategory == null)
            {
                ModelState.AddModelError(
                    nameof(model.CustomCategoryId),
                    "Invalid custom category.");
            }
        }

        if (!ModelState.IsValid)
        {
            model.CustomCategories = await GetCustomCategoriesAsync();
            return View(model);
        }

        var place = new Place
        {
            Name = model.Name.Trim(),
            Latitude = model.Latitude!.Value,
            Longitude = model.Longitude!.Value,
            Description = string.IsNullOrWhiteSpace(model.Description)
                ? null
                : model.Description.Trim(),
            SystemCategory = model.SystemCategory,
            CustomCategoryId = customCategory?.Id,
            CreatedByUserId = userId,
            CreatedAt = DateTime.Now
        };

        _db.Places.Add(place);

        if (!string.IsNullOrWhiteSpace(model.InitialPostContent))
        {
            var initialPost = new Post
            {
                Content = model.InitialPostContent.Trim(),
                AuthorId = userId,
                CreatedAt = DateTime.Now,
                Place = place
            };

            _db.Posts.Add(initialPost);
        }

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = place.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, string sort = "newest")
    {
        var place = await _db.Places
            .AsNoTracking()
            .Include(p => p.CreatedByUser)
            .Include(p => p.CustomCategory)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (place == null)
            return NotFound();

        var postsQuery = _db.Posts
            .AsNoTracking()
            .Where(p => p.PlaceId == id)
            .Include(p => p.Author)
            .Include(p => p.Comments)
            .AsQueryable();

        postsQuery = sort switch
        {
            "oldest" => postsQuery
                .OrderBy(p => p.CreatedAt),

            "most-liked" => postsQuery
                .OrderByDescending(p =>
                    _db.Likes.Count(l => l.PostId == p.Id && l.IsLike))
                .ThenByDescending(p => p.CreatedAt),

            "most-discussed" => postsQuery
                .OrderByDescending(p => p.Comments.Count)
                .ThenByDescending(p => p.CreatedAt),

            _ => postsQuery
                .OrderByDescending(p => p.CreatedAt)
        };

        var normalizedSort = sort switch
        {
            "oldest" => "oldest",
            "most-liked" => "most-liked",
            "most-discussed" => "most-discussed",
            _ => "newest"
        };

        var posts = await postsQuery
            .Select(p => new PlacePostListItemViewModel
            {
                Post = p,
                LikeCount = _db.Likes.Count(l =>
                    l.PostId == p.Id &&
                    l.IsLike),
                CommentCount = p.Comments.Count
            })
            .ToListAsync();

        var model = new PlaceDetailsViewModel
        {
            Place = place,
            Posts = posts,
            PostCount = posts.Count,
            Sort = normalizedSort,

            // Rating persistence will be connected in the rating task.
            AverageRating = null,
            RatingCount = 0
        };

        return View(model);
    }

    [HttpGet]
[Authorize]
public async Task<IActionResult> Delete(int id)
{
    var place = await _db.Places
        .AsNoTracking()
        .Include(p => p.CreatedByUser)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (place == null)
        return NotFound();

    var currentUserId = _userManager.GetUserId(User);
    if (currentUserId == null)
        return Forbid();

    var isAdmin = User.IsInRole("Admin");
    var isCreator = place.CreatedByUserId == currentUserId;

    if (!isCreator && !isAdmin)
        return Forbid();

    if (isCreator && !isAdmin)
    {
        var hasForeignPosts = await _db.Posts
            .AsNoTracking()
            .AnyAsync(p =>
                p.PlaceId == id &&
                p.AuthorId != currentUserId);

        if (hasForeignPosts)
            return Forbid();
    }

    return View(place);
}

[HttpPost, ActionName("Delete")]
[Authorize]
public async Task<IActionResult> DeleteConfirmed(int id)
{
    var place = await _db.Places
        .FirstOrDefaultAsync(p => p.Id == id);

    if (place == null)
        return NotFound();

    var currentUserId = _userManager.GetUserId(User);
    if (currentUserId == null)
        return Forbid();

    var isAdmin = User.IsInRole("Admin");
    var isCreator = place.CreatedByUserId == currentUserId;

    if (!isCreator && !isAdmin)
        return Forbid();

    var posts = await _db.Posts
        .Where(p => p.PlaceId == id)
        .ToListAsync();

    if (isCreator &&
        !isAdmin &&
        posts.Any(p => p.AuthorId != currentUserId))
    {
        return Forbid();
    }

    await using var transaction =
        await _db.Database.BeginTransactionAsync();

    try
    {
        if (posts.Count > 0)
            _db.Posts.RemoveRange(posts);

        _db.Places.Remove(place);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }

    return RedirectToAction(nameof(Index), "Home");
}

    private async Task<IReadOnlyList<CustomCategory>> GetCustomCategoriesAsync()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Array.Empty<CustomCategory>();

        return await _db.CustomCategories
            .Where(c => c.CreatedByUserId == userId && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}
