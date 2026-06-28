using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PROJECT2106.Data;
using PROJECT2106.Models;

namespace PROJECT2106.Controllers;

public class ProfileController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _db;

    public ProfileController(UserManager<AppUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<IActionResult> Index(string username)
    {
        // Тимчасово — потім з БД
        var user = await _userManager.FindByNameAsync(username);
        if (user == null)
        {
            return NotFound();
        }
        var posts = await _db.Posts
            .Where(p => p.AuthorId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        ViewBag.Posts = posts;
        var currentUserId = _userManager.GetUserId(User);
        ViewBag.IsFollowing = await _db.Follows
        .AnyAsync(f => f.FollowerId == currentUserId && f.FollowingId == user.Id);
        ViewBag.PostsCount = await _db.Posts.CountAsync(p => p.AuthorId == user.Id);
        ViewBag.FollowersCount = await _db.Follows.CountAsync(f => f.FollowingId == user.Id);
        ViewBag.FollowingCount = await _db.Follows.CountAsync(f => f.FollowerId == user.Id);
        ViewBag.LikesCount = await _db.Likes.CountAsync(l => l.UserId == user.Id && l.IsLike == true);
        return View(user);
    }

    public async Task<IActionResult> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return View(new List<AppUser>());
        }

        var users = await _userManager.Users
            .Where(u => u.UserName.Contains(query))
            .ToListAsync();

        return View(users);
    }
}