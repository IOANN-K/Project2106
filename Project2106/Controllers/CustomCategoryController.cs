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
    private readonly IWebHostEnvironment _environment;

    private const long MaxIconSizeBytes = 2 * 1024 * 1024;
    private const string IconUrlPrefix = "/uploads/category-icons/";

    private static readonly IReadOnlyDictionary<string, string> AllowedIconTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp"
        };

    public CustomCategoryController(
        AppDbContext db,
        UserManager<AppUser> userManager,
        IWebHostEnvironment environment)
    {
        _db = db;
        _userManager = userManager;
        _environment = environment;
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
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Forbid();

        var name = model.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(nameof(model.Name), "Category name is required.");
            return View(model);
        }

        var iconExtension = model.Icon == null
            ? null
            : ValidateIcon(model.Icon);

        if (!ModelState.IsValid)
            return View(model);

        string? iconPath = null;
        string? physicalIconPath = null;

        if (model.Icon != null && iconExtension != null)
        {
            try
            {
                (iconPath, physicalIconPath) = await SaveIconAsync(model.Icon, iconExtension);
            }
            catch (IOException)
            {
                ModelState.AddModelError(nameof(model.Icon), "The category icon could not be saved. Please try again.");
                return View(model);
            }
            catch (UnauthorizedAccessException)
            {
                ModelState.AddModelError(nameof(model.Icon), "The category icon could not be saved. Please try again.");
                return View(model);
            }
        }

        var category = new CustomCategory
        {
            Name = name,
            CreatedByUserId = userId,
            CreatedAt = DateTime.Now,
            IsActive = true,
            IconPath = iconPath
        };

        _db.CustomCategories.Add(category);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch
        {
            DeleteFileIfPresent(physicalIconPath);
            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Forbid();

        var category = await _db.CustomCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                c.CreatedByUserId == userId &&
                c.IsActive);

        if (category == null)
            return NotFound();

        return View(new CustomCategoryEditViewModel
        {
            Id = category.Id,
            Name = category.Name,
            ExistingIconPath = category.IconPath
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(CustomCategoryEditViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Forbid();

        var category = await _db.CustomCategories
            .FirstOrDefaultAsync(c =>
                c.Id == model.Id &&
                c.CreatedByUserId == userId &&
                c.IsActive);

        if (category == null)
            return NotFound();

        model.ExistingIconPath = category.IconPath;
        var name = model.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
            ModelState.AddModelError(nameof(model.Name), "Category name is required.");

        var iconExtension = model.Icon == null
            ? null
            : ValidateIcon(model.Icon);

        if (!ModelState.IsValid)
            return View(model);

        string? newIconUrl = null;
        string? newIconPath = null;

        if (model.Icon != null && iconExtension != null)
        {
            try
            {
                (newIconUrl, newIconPath) = await SaveIconAsync(model.Icon, iconExtension);
            }
            catch (IOException)
            {
                ModelState.AddModelError(nameof(model.Icon), "The category icon could not be saved. Please try again.");
                return View(model);
            }
            catch (UnauthorizedAccessException)
            {
                ModelState.AddModelError(nameof(model.Icon), "The category icon could not be saved. Please try again.");
                return View(model);
            }
        }

        var previousIconPath = category.IconPath;
        category.Name = name;

        if (newIconUrl != null)
            category.IconPath = newIconUrl;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch
        {
            DeleteFileIfPresent(newIconPath);
            throw;
        }

        if (newIconUrl != null)
            DeleteManagedIcon(previousIconPath);

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

    private string? ValidateIcon(IFormFile file)
    {
        if (file.Length <= 0)
        {
            ModelState.AddModelError("Icon", "The selected icon is empty.");
            return null;
        }

        if (file.Length > MaxIconSizeBytes)
        {
            ModelState.AddModelError("Icon", "Category icons cannot exceed 2 MB.");
            return null;
        }

        var extension = Path.GetExtension(Path.GetFileName(file.FileName)).ToLowerInvariant();

        if (!AllowedIconTypes.TryGetValue(extension, out var expectedMimeType) ||
            !string.Equals(file.ContentType, expectedMimeType, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Icon", "Use a JPG, PNG, or WebP image.");
            return null;
        }

        return extension;
    }

    private async Task<(string Url, string PhysicalPath)> SaveIconAsync(
        IFormFile icon,
        string extension)
    {
        var directory = GetIconDirectory();
        Directory.CreateDirectory(directory);

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(directory, storedFileName);

        try
        {
            await using var stream = new FileStream(
                physicalPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);

            await icon.CopyToAsync(stream);
        }
        catch
        {
            DeleteFileIfPresent(physicalPath);
            throw;
        }

        return (IconUrlPrefix + storedFileName, physicalPath);
    }

    private string GetIconDirectory()
    {
        var webRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;

        return Path.Combine(webRoot, "uploads", "category-icons");
    }

    private void DeleteManagedIcon(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl) ||
            !relativeUrl.StartsWith(IconUrlPrefix, StringComparison.Ordinal))
        {
            return;
        }

        var fileName = relativeUrl[IconUrlPrefix.Length..];
        if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName))
            return;

        var directory = Path.GetFullPath(GetIconDirectory());
        var candidate = Path.GetFullPath(Path.Combine(directory, fileName));

        if (!candidate.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            return;

        DeleteFileIfPresent(candidate);
    }

    private static void DeleteFileIfPresent(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
        catch (IOException)
        {
            // File cleanup is best-effort after the database update.
        }
        catch (UnauthorizedAccessException)
        {
            // File cleanup is best-effort after the database update.
        }
    }
}
