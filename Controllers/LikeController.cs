using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PROJECT2106.Data;
using PROJECT2106.Models;

namespace PROJECT2106.Controllers;

[Authorize]
public class LikeController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public LikeController(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }
    
    [HttpPost]
    public async Task<IActionResult> ToggleLike(int postId, bool isLike)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Forbid();

        var existing = await _db.Likes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

        if (existing != null)
        {
            if (existing.IsLike == isLike)
                _db.Likes.Remove(existing); 
            else
                existing.IsLike = isLike; 
        }
        else
        {
            _db.Likes.Add(new Like { PostId = postId, UserId = userId, IsLike = isLike });
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            // Another concurrent request already created the reaction.
            // The unique index guarantees one reaction per user/post.
        }

        return RedirectToAction("Details", "Post", new { id = postId });
    }
}