using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PROJECT2106.Data;
using PROJECT2106.Models;

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
    public async Task<IActionResult> AddComment(int postId, string content, int? parentCommentId)
    {
        var post = await _db.Posts.FindAsync(postId);
        if (post == null) return NotFound();

        var userId = _userManager.GetUserId(User);
        if (userId == null) return Forbid();

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
        _logger.LogInformation("Added comment #{CommentId} to post #{PostId}", comment.Id, postId);

        return RedirectToAction(nameof(Details), new { id = postId });
    }

    // GET /Post/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST /Post/Create
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(Post post, string tags)
    {
        if (ModelState.IsValid)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Forbid();

            post.CreatedAt = DateTime.Now;
            post.AuthorId = userId;
            _db.Posts.Add(post);

            if (!string.IsNullOrWhiteSpace(tags))
            {
                var tagNames = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(t => t.Trim().ToLowerInvariant())
                                   .Distinct();

                foreach (var tagName in tagNames)
                {
                    var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Name == tagName)
                          ?? new Tag { Name = tagName };
                    post.Tags.Add(tag);
                }
            }
            await _db.SaveChangesAsync();
            _logger.LogInformation("Created post #{Id}", post.Id);
            return RedirectToAction(nameof(Index));
        }
        return View(post);
    }

    // GET /Post/Edit/1
    public async Task<IActionResult> Edit(int id)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post == null) return NotFound();
        var currentUserId = _userManager.GetUserId(User);
        if (post.AuthorId != currentUserId && !User.IsInRole("Admin"))
        return Forbid();
        return View(post);
    }

    // POST /Post/Edit/1
    [HttpPost]
    [HttpPost]
    public async Task<IActionResult> Edit(int id, Post post)
    {
        if (id != post.Id) return NotFound();
        var dbPost = await _db.Posts.FindAsync(id);
        if (dbPost == null) return NotFound();
        var currentUserId = _userManager.GetUserId(User);
        if (dbPost.AuthorId != currentUserId && !User.IsInRole("Admin"))
            return Forbid();
        if (ModelState.IsValid)
        {
            _db.Update(post);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Updated post #{Id}", post.Id);
            return RedirectToAction(nameof(Index));
        }
        return View(post);
    }

    // GET /Post/Delete/1
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
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post == null) return NotFound();
        var currentUserId = _userManager.GetUserId(User);
        if (post.AuthorId != currentUserId && !User.IsInRole("Admin"))
            return Forbid();
        _db.Posts.Remove(post);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Deleted post #{Id}", id);
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
    public async Task<IActionResult> EditComment(int commentId, string content)
    {
        var comment = await _db.Comments.FindAsync(commentId);
        if (comment == null) return NotFound();
        var currentUserId = _userManager.GetUserId(User);
        if (comment.AuthorId != currentUserId && !User.IsInRole("Admin"))
            return Forbid();
        comment.Content = content;
        comment.IsEdited = true;
        comment.EditedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Edited comment #{CommentId}", commentId);
        return RedirectToAction(nameof(Details), new { id = comment.PostId });
    }[Authorize]
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