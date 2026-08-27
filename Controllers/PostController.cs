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

    public PostController(AppDbContext db, ILogger<PostController> logger, UserManager<AppUser> userManager)
    {
        _db = db;
        _logger = logger;
        _userManager = userManager;
    }

    // GET /Post
    public async Task<IActionResult> Index(string? tag)
    {
        var query = _db.Posts
            .Include(p => p.Author)
            .Include(p => p.Comments)
            .Include(p => p.Tags)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(p => p.Tags.Any(t => t.Name == tag.ToLower()));

        var posts = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        ViewBag.CurrentTag = tag;
        return View(posts);
    }

    // GET /Post/Details/1
    public async Task<IActionResult> Details(int id)
    {
        var post = await _db.Posts
            .Include(p => p.Author)
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
                return BadRequest("Parent comment belongs to another post.");
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
        if (placeId.HasValue)
        {
            var placeExists = await _db.Places
                .AsNoTracking()
                .AnyAsync(p => p.Id == placeId.Value);

            if (!placeExists)
                return NotFound();
        }

        return View(new PostCreateViewModel
        {
            PlaceId = placeId
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

        if (model.PlaceId.HasValue)
        {
            var placeExists = await _db.Places
                .AsNoTracking()
                .AnyAsync(p => p.Id == model.PlaceId.Value);

            if (!placeExists)
            {
                ModelState.AddModelError(
                    nameof(model.PlaceId),
                    "Selected place does not exist.");
            }
        }

        if (!ModelState.IsValid)
            return View(model);

        var post = new Post
        {
            Content = model.Content.Trim(),
            CreatedAt = DateTime.Now,
            AuthorId = userId,
            PlaceId = model.PlaceId
        };

        _db.Posts.Add(post);

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

        await _db.SaveChangesAsync();

        _logger.LogInformation("Created post #{Id}", post.Id);

        if (post.PlaceId.HasValue)
        {
            return RedirectToAction(
                "Details",
                "Place",
                new { id = post.PlaceId.Value });
        }

        return RedirectToAction(nameof(Index));
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

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated post #{Id}", dbPost.Id);

        return RedirectToAction(nameof(Index));
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
        var postId = comment.PostId; // Store the PostId before deleting the comment
        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Deleted comment #{CommentId}", commentId);
        return RedirectToAction(nameof(Details), new { id = comment.PostId });
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
    public async Task<IActionResult> Feed()
    {
        var userId = _userManager.GetUserId(User);

        var followingIds = await _db.Follows
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowingId)
            .ToListAsync();

        var posts = await _db.Posts
            .Include(p => p.Author)
            .Include(p => p.Comments)
            .Include(p => p.Tags)
            .Where(p => followingIds.Contains(p.AuthorId!))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View(posts);
    }
}
