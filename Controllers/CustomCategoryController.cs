using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECT2106.Data;
using PROJECT2106.Models;
using PROJECT2106.ViewModels;

namespace PROJECT2106.Controllers;

[Authorize]
public class CustomCategoryController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public CustomCategoryController(
        AppDbContext db,
        UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Forbid();

        var categories = await _db.CustomCategories
            .Where(c => c.CreatedByUserId == userId && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return View(categories);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CustomCategoryCreateViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CustomCategoryCreateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Forbid();

        var name = model.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(nameof(model.Name), "Category name is required.");
            return View(model);
        }

        var category = new CustomCategory
        {
            Name = name,
            CreatedByUserId = userId,
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        _db.CustomCategories.Add(category);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Forbid();

        var category = await _db.CustomCategories
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                c.CreatedByUserId == userId);

        if (category == null)
            return NotFound();

        category.IsActive = false;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
