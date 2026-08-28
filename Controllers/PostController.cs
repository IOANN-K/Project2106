using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PROJECT2106.Data;
using PROJECT2106.Models;
using PROJECT2106.ViewModels;

namespace PROJECT2106.Controllers;

public class PostController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<PostController> _logger;

    private const int MaxMediaFiles = 10;
    private const long MaxImageSizeBytes = 10 * 1024 * 1024;
    private const long MaxVideoSizeBytes = 50 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, PostMediaType>
        AllowedMediaTypes =
            new Dictionary<string, PostMediaType>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["image/jpeg"] = PostMediaType.Image,
                ["image/png"] = PostMediaType.Image,
                ["image/webp"] = PostMediaType.Image,
                ["video/mp4"] = PostMediaType.Video,
                ["video/webm"] = PostMediaType.Video
            };

    private static readonly HashSet<string> AllowedImageExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

    private static readonly HashSet<string> AllowedVideoExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4",
            ".webm"
        };

    public PostController(AppDbContext db, ILogger<PostController> logger, UserManager<AppUser> userManager)
    {
        _db = db;
        _logger = logger;
        _userManager = userManager;
    }

    private static string? ValidateMediaFile(IFormFile file)
    {
        if (file.Length <= 0)
            return "Empty media files are not allowed.";

        if (!AllowedMediaTypes.TryGetValue(
                file.ContentType,
                out var mediaType))
        {
            return $"Unsupported media type: {file.ContentType}.";
        }

        var extension =
            Path.GetExtension(
                Path.GetFileName(file.FileName));

        var validExtension = mediaType switch
        {
            PostMediaType.Image =>
                AllowedImageExtensions.Contains(extension),

            PostMediaType.Video =>
                AllowedVideoExtensions.Contains(extension),

            _ => false
        };

        if (!validExtension)
            return $"Unsupported file extension: {extension}.";

        var maxSize = mediaType == PostMediaType.Image
            ? MaxImageSizeBytes
            : MaxVideoSizeBytes;

        if (file.Length > maxSize)
        {
            return mediaType == PostMediaType.Image
                ? "Image cannot exceed 10 MB."
                : "Video cannot exceed 50 MB.";
        }

        return null;
    }

    // GET /Post
    public async Task<IActionResult> Index(
        string? tag,
        int page = 1)
    {
        const int pageSize = 20;

        page = Math.Max(page, 1);

        var normalizedTag = tag?
            .Trim()
            .ToLowerInvariant();

        var query = _db.Posts
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalizedTag))
        {
            query = query.Where(p =>
                p.Tags.Any(t => t.Name == normalizedTag));
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PostListItemViewModel
            {
                Id = p.Id,
                AuthorId = p.AuthorId,

                AuthorUsername =
                    p.Author != null &&
                    p.Author.UserName != null
                        ? p.Author.UserName
                        : "Unknown",

                AuthorAvatarUrl = p.Author != null
                    ? p.Author.AvatarUrl
                    : null,

                PlaceId = p.PlaceId,
                PlaceName = p.Place != null ? p.Place.Name : null,

                Content = p.Content,
                CreatedAt = p.CreatedAt,

                CommentCount = _db.Comments.Count(c =>
                    c.PostId == p.Id),

                Tags = p.Tags
                    .OrderBy(t => t.Name)
                    .Select(t => t.Name)
                    .ToList()
            })
            .ToListAsync();

        ViewBag.CurrentTag = normalizedTag;

        return View(new PagedResult<PostListItemViewModel>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        });
    }

    // GET /Post/Details/1
    public async Task<IActionResult> Details(int id)
    {
        var post = await _db.Posts
            .Include(p => p.Author)
            .Include(p => p.Media.OrderBy(m => m.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null) return NotFound();

        post.Comments = await _db.Comments
            .Include(c => c.Author)
            .Where(c => c.PostId == id)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        
        var lookup = post.Comments.ToLookup(c => c.ParentCommentId);
        foreach (var comment in post.Comments)
            comment.Replies = lookup[comment.Id].ToList();

        ViewBag.Likes = await _db.Likes.CountAsync(l => l.PostId == id && l.IsLike == true);
        ViewBag.Dislikes = await _db.Likes.CountAsync(l => l.PostId == id && l.IsLike == false);
        return View(post);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddComment(
        int postId,
        CommentInputViewModel model,
        int? parentCommentId)
    {
        var post = await _db.Posts.FindAsync(postId);
        if (post == null)
            return NotFound();

        var content = model.Content?.Trim();

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(content))
        {
            TempData["CommentError"] =
                ModelState.Values
                    .SelectMany(value => value.Errors)
                    .Select(error => error.ErrorMessage)
                    .FirstOrDefault()
                ?? "Comment content is required.";

            return RedirectToAction(nameof(Details), new { id = postId });
        }

        if (content.Length > 2000)
        {
            TempData["CommentError"] =
                "Comment cannot exceed 2000 characters.";

            return RedirectToAction(nameof(Details), new { id = postId });
        }

        if (parentCommentId.HasValue)
        {
            var parentComment = await _db.Comments
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == parentCommentId.Value);

            if (parentComment == null)
                return BadRequest("Parent comment does not exist.");

            if (parentComment.PostId != postId)
                return BadRequest("Parent comment belongs to another contribution.");
        }

        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Forbid();

        var comment = new Comment
        {
            PostId = postId,
            Content = content,
            AuthorId = userId,
            ParentCommentId = parentCommentId,
            CreatedAt = DateTime.Now
        };

        _db.Comments.Add(comment);

        if (post.AuthorId != null &&
            post.AuthorId != userId)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = post.AuthorId,
                ActorUserId = userId,
                Type = NotificationType.PostCommented,
                PostId = post.Id,
                PlaceId = post.PlaceId,
                CreatedAt = DateTime.Now
            });
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Added comment #{CommentId} to post #{PostId}",
            comment.Id,
            postId);

        return RedirectToAction(nameof(Details), new { id = postId });
    }

    // GET /Post/Create
    [Authorize]
    public async Task<IActionResult> Create(int? placeId)
    {
        if (!placeId.HasValue)
        {
            return RedirectToAction("Index", "Map");
        }

        var place = await _db.Places
            .AsNoTracking()
            .Where(p => p.Id == placeId.Value)
            .Select(p => new
            {
                p.Id,
                p.Name
            })
            .FirstOrDefaultAsync();

        if (place == null)
            return NotFound();

        return View(new PostCreateViewModel
        {
            PlaceId = place.Id,
            PlaceName = place.Name
        });
    }

    // POST /Post/Create
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(PostCreateViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Forbid();

        string? placeName = null;

        if (!model.PlaceId.HasValue)
        {
            ModelState.AddModelError(
                nameof(model.PlaceId),
                "Place is required.");
        }
        else
        {
            placeName = await _db.Places
                .AsNoTracking()
                .Where(p => p.Id == model.PlaceId.Value)
                .Select(p => p.Name)
                .FirstOrDefaultAsync();

            if (placeName == null)
            {
                ModelState.AddModelError(
                    nameof(model.PlaceId),
                    "Selected place does not exist.");
            }
        }

        if (model.MediaFiles.Count > MaxMediaFiles)
        {
            ModelState.AddModelError(
                nameof(model.MediaFiles),
                $"A contribution can contain at most {MaxMediaFiles} media files.");
        }

        foreach (var file in model.MediaFiles)
        {
            var error = ValidateMediaFile(file);

            if (error != null)
            {
                ModelState.AddModelError(
                    nameof(model.MediaFiles),
                    error);
            }
        }

        if (!ModelState.IsValid)
        {
            model.PlaceName = placeName ?? string.Empty;
            return View(model);
        }

        var post = new Post
        {
            Content = model.Content.Trim(),
            CreatedAt = DateTime.Now,
            AuthorId = userId,
            PlaceId = model.PlaceId!.Value
        };

        _db.Posts.Add(post);

        var followerIds = await _db.Follows
            .AsNoTracking()
            .Where(f => f.FollowingId == userId)
            .Select(f => f.FollowerId)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(model.Tags))
        {
            var tagNames = model.Tags
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(tag => tag.Trim().ToLowerInvariant())
                .Where(tag => tag.Length > 0)
                .Distinct();

            foreach (var tagName in tagNames)
            {
                var tag =
                    await _db.Tags.FirstOrDefaultAsync(t => t.Name == tagName)
                    ?? new Tag { Name = tagName };

                post.Tags.Add(tag);
            }
        }

        var uploadedPaths = new List<string>();

        var uploadDirectory = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "uploads",
            "posts");

        Directory.CreateDirectory(uploadDirectory);

        try
        {
            for (var index = 0; index < model.MediaFiles.Count; index++)
            {
                var file = model.MediaFiles[index];

                var mediaType = AllowedMediaTypes[file.ContentType];

                var extension = Path
                    .GetExtension(Path.GetFileName(file.FileName))
                    .ToLowerInvariant();

                var storedFileName =
                    $"{Guid.NewGuid():N}{extension}";

                var absolutePath = Path.Combine(
                    uploadDirectory,
                    storedFileName);

                await using (var stream =
                    new FileStream(
                        absolutePath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None))
                {
                    await file.CopyToAsync(stream);
                }

                uploadedPaths.Add(absolutePath);

                post.Media.Add(new PostMedia
                {
                    MediaType = mediaType,

                    OriginalFileName =
                        Path.GetFileName(file.FileName),

                    StoredFileName = storedFileName,

                    RelativePath =
                        $"/uploads/posts/{storedFileName}",

                    MimeType = file.ContentType,

                    SizeBytes = file.Length,

                    SortOrder = index,

                    CreatedAt = DateTime.Now
                });
            }

            foreach (var followerId in followerIds)
            {
                if (followerId == userId)
                    continue;

                _db.Notifications.Add(new Notification
                {
                    UserId = followerId,
                    ActorUserId = userId,
                    Type = NotificationType.FollowedUserPosted,
                    Post = post,
                    PlaceId = post.PlaceId,
                    CreatedAt = DateTime.Now
                });
            }

            await _db.SaveChangesAsync();
        }
        catch
        {
            foreach (var path in uploadedPaths)
            {
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }

            throw;
        }

        _logger.LogInformation("Created post #{Id}", post.Id);

        return RedirectToAction(
            "Details",
            "Place",
            new { id = post.PlaceId!.Value });
    }

    // GET /Post/Edit/1
    [Authorize]
    public async Task<IActionResult> Edit(int id)
    {
        var post = await _db.Posts
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null)
            return NotFound();

        var currentUserId = _userManager.GetUserId(User);

        if (post.AuthorId != currentUserId && !User.IsInRole("Admin"))
            return Forbid();

        var model = new PostEditViewModel
        {
            Id = post.Id,
            Content = post.Content
        };

        return View(model);
    }

    // POST /Post/Edit/1
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Edit(int id, PostEditViewModel model)
    {
        if (id != model.Id)
            return NotFound();

        var dbPost = await _db.Posts.FindAsync(id);

        if (dbPost == null)
            return NotFound();

        var currentUserId = _userManager.GetUserId(User);

        if (dbPost.AuthorId != currentUserId && !User.IsInRole("Admin"))
            return Forbid();

        if (!ModelState.IsValid)
            return View(model);

        dbPost.Content = model.Content.Trim();
        dbPost.IsEdited = true;
        dbPost.EditedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated post #{Id}", dbPost.Id);

        if (dbPost.PlaceId.HasValue)
        {
            return RedirectToAction(
                "Details",
                "Place",
                new { id = dbPost.PlaceId.Value });
        }

        return RedirectToAction(nameof(Details), new { id = dbPost.Id });
    }

    // GET /Post/Delete/1
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post == null) return NotFound();
        var currentUserId = _userManager.GetUserId(User);
        if (post.AuthorId != currentUserId && !User.IsInRole("Admin"))
            return Forbid();
        return View(post);
    }

    // POST /Post/Delete/1
    [HttpPost, ActionName("Delete")]
    [Authorize]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post == null) return NotFound();
        var currentUserId = _userManager.GetUserId(User);
        if (post.AuthorId != currentUserId && !User.IsInRole("Admin"))
            return Forbid();
        var placeId = post.PlaceId;
        _db.Posts.Remove(post);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Deleted post #{Id}", id);

        if (placeId.HasValue)
        {
            return RedirectToAction(
                "Details",
                "Place",
                new { id = placeId.Value });
        }

        return RedirectToAction(nameof(Index));
    }

    // Delete a comment
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        var comment = await _db.Comments.FindAsync(commentId);
        if (comment == null) return NotFound();
        var currentUserId = _userManager.GetUserId(User);
        if (comment.AuthorId != currentUserId && !User.IsInRole("Admin"))
            return Forbid();
        var postId = comment.PostId;

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Deleted comment #{CommentId}",
            commentId);

        return RedirectToAction(
            nameof(Details),
            new { id = postId });
    }

    // Edit a comment
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> EditComment(
        int commentId,
        CommentInputViewModel model)
    {
        var comment = await _db.Comments.FindAsync(commentId);

        if (comment == null)
            return NotFound();

        var currentUserId = _userManager.GetUserId(User);

        if (comment.AuthorId != currentUserId && !User.IsInRole("Admin"))
            return Forbid();

        var content = model.Content?.Trim();

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(content))
        {
            TempData["CommentError"] =
                ModelState.Values
                    .SelectMany(value => value.Errors)
                    .Select(error => error.ErrorMessage)
                    .FirstOrDefault()
                ?? "Comment content is required.";

            return RedirectToAction(
                nameof(Details),
                new { id = comment.PostId });
        }

        if (content.Length > 2000)
        {
            TempData["CommentError"] =
                "Comment cannot exceed 2000 characters.";

            return RedirectToAction(
                nameof(Details),
                new { id = comment.PostId });
        }

        comment.Content = content;
        comment.IsEdited = true;
        comment.EditedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Edited comment #{CommentId}",
            commentId);

        return RedirectToAction(
            nameof(Details),
            new { id = comment.PostId });
    }

    [Authorize]
    public async Task<IActionResult> Feed(int page = 1)
    {
        const int pageSize = 20;
        page = Math.Max(page, 1);

        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Forbid();

        var query = _db.Posts
            .AsNoTracking()
            .Where(p =>
                p.PlaceId.HasValue &&
                p.AuthorId != null &&
                _db.Follows.Any(f =>
                    f.FollowerId == userId &&
                    f.FollowingId == p.AuthorId));

        var totalItems = await query.CountAsync();

        var feed = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new FeedItemViewModel
            {
                PostId = p.Id,

                AuthorUsername =
                    p.Author != null &&
                    p.Author.UserName != null
                        ? p.Author.UserName
                        : "Unknown explorer",

                AuthorAvatarUrl = p.Author != null
                    ? p.Author.AvatarUrl
                    : null,

                PlaceId = p.PlaceId!.Value,

                PlaceName =
                    p.Place != null
                        ? p.Place.Name
                        : "Unknown place",

                Category =
                    p.Place != null &&
                    p.Place.SystemCategory.HasValue
                        ? p.Place.SystemCategory.Value.ToString()
                        : p.Place != null &&
                          p.Place.CustomCategory != null
                            ? p.Place.CustomCategory.Name
                            : "Other",

                Excerpt = p.Content.Length > 220
                    ? p.Content.Substring(0, 220) + "…"
                    : p.Content,

                CreatedAt = p.CreatedAt,
                IsEdited = p.IsEdited,

                LikeCount = _db.Likes.Count(l =>
                    l.PostId == p.Id &&
                    l.IsLike),

                CommentCount = _db.Comments.Count(c =>
                    c.PostId == p.Id),

                Media = p.Media
                    .OrderBy(m => m.SortOrder)
                    .ThenBy(m => m.Id)
                    .Select(m => new FeedMediaPreviewViewModel
                    {
                        RelativePath = m.RelativePath,
                        OriginalFileName = m.OriginalFileName,
                        MimeType = m.MimeType,
                        MediaType = m.MediaType
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        return View(new PagedResult<FeedItemViewModel>
        {
            Items = feed,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        });
    }
}
