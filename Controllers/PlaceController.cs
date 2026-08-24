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
    public async Task<IActionResult> Details(int id)
    {
        var place = await _db.Places
            .Include(p => p.CreatedByUser)
            .Include(p => p.CustomCategory)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (place == null)
            return NotFound();

        return View(place);
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
