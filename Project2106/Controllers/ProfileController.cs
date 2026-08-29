using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PROJECT2106.Data;
using PROJECT2106.Models;
using PROJECT2106.ViewModels;

namespace PROJECT2106.Controllers;

public class ProfileController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment? _environment;

    private const long MaxAvatarSizeBytes = 5 * 1024 * 1024;
    private const string AvatarUrlPrefix = "/uploads/avatars/";

    private static readonly IReadOnlyDictionary<string, string> AllowedAvatarTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp"
        };

    public ProfileController(
        UserManager<AppUser> userManager,
        AppDbContext db,
        IWebHostEnvironment? environment = null)
    {
        _userManager = userManager;
        _db = db;
        _environment = environment;
    }

    public async Task<IActionResult> Index(
        string username,
        int page = 1)
    {
        if (string.IsNullOrWhiteSpace(username))
            return NotFound();

        var user = await _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == username);

        if (user == null)
            return NotFound();

        const int contributionPageSize = 10;
        page = Math.Max(page, 1);

        var contributionsCount = await _db.Posts
            .AsNoTracking()
            .CountAsync(p => p.AuthorId == user.Id);

        var contributions = await _db.Posts
            .AsNoTracking()
            .Where(p => p.AuthorId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * contributionPageSize)
            .Take(contributionPageSize)
            .Select(p => new ProfileContributionCardViewModel
            {
                Id = p.Id,
                Excerpt = p.Content.Length > 280
                    ? p.Content.Substring(0, 280) + "…"
                    : p.Content,
                CreatedAt = p.CreatedAt,
                IsEdited = p.IsEdited,
                PlaceId = p.PlaceId,
                PlaceName = p.Place != null ? p.Place.Name : null,
                PreviewImageUrl = p.Media
                    .Where(m => m.MediaType == PostMediaType.Image)
                    .OrderBy(m => m.SortOrder)
                    .ThenBy(m => m.Id)
                    .Select(m => m.RelativePath)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var createdPlacesCount = await _db.Places
            .AsNoTracking()
            .CountAsync(p => p.CreatedByUserId == user.Id);

        var createdPlaces = await _db.Places
            .AsNoTracking()
            .Where(p => p.CreatedByUserId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Take(12)
            .Select(p => new ProfilePlaceCardViewModel
            {
                Id = p.Id,
                Name = p.Name,
                SystemCategory = p.SystemCategory,
                CustomCategoryName = p.CustomCategory != null
                    ? p.CustomCategory.Name
                    : null,
                CreatedAt = p.CreatedAt,
                PreviewImageUrl = p.Posts
                    .SelectMany(post => post.Media)
                    .Where(media => media.MediaType == PostMediaType.Image)
                    .OrderByDescending(media => media.Post!.CreatedAt)
                    .ThenBy(media => media.SortOrder)
                    .ThenBy(media => media.Id)
                    .Select(media => media.RelativePath)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var followersCount = await _db.Follows
            .CountAsync(f => f.FollowingId == user.Id);

        var followingCount = await _db.Follows
            .CountAsync(f => f.FollowerId == user.Id);

        var likesReceived = await _db.Likes
            .CountAsync(l =>
                l.IsLike &&
                l.UserId != user.Id &&
                l.Post != null &&
                l.Post.AuthorId == user.Id);

        var commentsCreated = await _db.Comments
            .CountAsync(c => c.AuthorId == user.Id);

        var reputation =
            createdPlacesCount * 10 +
            contributionsCount * 5 +
            likesReceived * 2 +
            commentsCreated;

        var explorerLevel = reputation switch
        {
            >= 1000 => "Level 5",
            >= 400 => "Level 4",
            >= 150 => "Level 3",
            >= 50 => "Level 2",
            _ => "Level 1"
        };

        var currentUserId = _userManager.GetUserId(User);

        var isOwnProfile =
            currentUserId != null &&
            currentUserId == user.Id;

        var isFollowing = false;

        if (currentUserId != null &&
            currentUserId != user.Id)
        {
            isFollowing = await _db.Follows
                .AnyAsync(f =>
                    f.FollowerId == currentUserId &&
                    f.FollowingId == user.Id);
        }

        var model = new ViewModels.ProfileViewModel
        {
            User = user,
            Contributions = contributions,
            CreatedPlaces = createdPlaces,
            ContributionsCount = contributionsCount,
            ContributionPage = page,
            ContributionPageSize = contributionPageSize,
            CreatedPlacesCount = createdPlacesCount,
            FollowersCount = followersCount,
            FollowingCount = followingCount,
            LikesReceived = likesReceived,
            CommentsCreated = commentsCreated,
            Reputation = reputation,
            ExplorerLevel = explorerLevel,
            IsFollowing = isFollowing,
            IsOwnProfile = isOwnProfile
        };

        return View(model);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        return View(new EditProfileViewModel
        {
            Bio = user.Bio,
            CurrentAvatarUrl = user.AvatarUrl,
            Username = user.UserName ?? string.Empty
        });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Edit(EditProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        model.Username = user.UserName ?? string.Empty;
        model.CurrentAvatarUrl = user.AvatarUrl;

        string? extension = null;
        if (model.Avatar != null)
        {
            extension = ValidateAvatar(model.Avatar);
        }

        if (!ModelState.IsValid)
            return View(model);

        string? newAvatarUrl = null;
        string? newAvatarPath = null;

        if (model.Avatar != null && extension != null)
        {
            try
            {
                var avatarDirectory = GetUploadDirectory("avatars");
                Directory.CreateDirectory(avatarDirectory);

                var storedFileName = $"{Guid.NewGuid():N}{extension}";
                newAvatarPath = Path.Combine(avatarDirectory, storedFileName);

                await using var stream = new FileStream(
                    newAvatarPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None);

                await model.Avatar.CopyToAsync(stream);
                newAvatarUrl = AvatarUrlPrefix + storedFileName;
            }
            catch (IOException)
            {
                DeleteFileIfPresent(newAvatarPath);
                ModelState.AddModelError(nameof(model.Avatar), "The profile photo could not be saved. Please try again.");
                return View(model);
            }
            catch (UnauthorizedAccessException)
            {
                DeleteFileIfPresent(newAvatarPath);
                ModelState.AddModelError(nameof(model.Avatar), "The profile photo could not be saved. Please try again.");
                return View(model);
            }
        }

        var previousAvatarUrl = user.AvatarUrl;
        user.Bio = model.Bio?.Trim() ?? string.Empty;

        if (newAvatarUrl != null)
            user.AvatarUrl = newAvatarUrl;

        IdentityResult result;

        try
        {
            result = await _userManager.UpdateAsync(user);
        }
        catch
        {
            DeleteFileIfPresent(newAvatarPath);
            throw;
        }

        if (!result.Succeeded)
        {
            DeleteFileIfPresent(newAvatarPath);
            user.AvatarUrl = previousAvatarUrl;

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            model.CurrentAvatarUrl = previousAvatarUrl;
            return View(model);
        }

        if (newAvatarUrl != null)
            DeleteManagedUpload(previousAvatarUrl, AvatarUrlPrefix, "avatars");

        TempData["StatusMessage"] = "Your profile has been updated.";

        return RedirectToAction(nameof(Index), new { username = user.UserName });
    }

    public async Task<IActionResult> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return View(new List<AppUser>());
        }

        var users = await _userManager.Users
            .Where(u =>
                u.UserName != null &&
                u.UserName.Contains(query))
            .ToListAsync();

        return View(users);
    }

    private string? ValidateAvatar(IFormFile file)
    {
        if (file.Length <= 0)
        {
            ModelState.AddModelError(nameof(EditProfileViewModel.Avatar), "The selected profile photo is empty.");
            return null;
        }

        if (file.Length > MaxAvatarSizeBytes)
        {
            ModelState.AddModelError(nameof(EditProfileViewModel.Avatar), "Profile photos cannot exceed 5 MB.");
            return null;
        }

        var extension = Path.GetExtension(Path.GetFileName(file.FileName)).ToLowerInvariant();

        if (!AllowedAvatarTypes.TryGetValue(extension, out var expectedMimeType) ||
            !string.Equals(file.ContentType, expectedMimeType, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(EditProfileViewModel.Avatar), "Use a JPG, PNG, or WebP image.");
            return null;
        }

        return extension;
    }

    private string GetUploadDirectory(string directoryName)
    {
        var webRoot = _environment?.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(_environment?.ContentRootPath ?? Directory.GetCurrentDirectory(), "wwwroot");

        return Path.Combine(webRoot, "uploads", directoryName);
    }

    private void DeleteManagedUpload(string? relativeUrl, string prefix, string directoryName)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl) ||
            !relativeUrl.StartsWith(prefix, StringComparison.Ordinal))
        {
            return;
        }

        var fileName = relativeUrl[prefix.Length..];
        if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName))
            return;

        var directory = Path.GetFullPath(GetUploadDirectory(directoryName));
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
