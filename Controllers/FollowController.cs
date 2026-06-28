using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECT2106.Data;
using PROJECT2106.Models;

namespace PROJECT2106.Controllers;

[Authorize]
public class FollowController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public FollowController(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<IActionResult> Follow(string username)
    {
        var currentUserId = _userManager.GetUserId(User);
        var userToFollow = await _userManager.FindByNameAsync(username);

        if (userToFollow == null || userToFollow.Id == currentUserId)
        {
            return BadRequest();
        }

        var existingFollow = await _db.Follows
            .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowingId == userToFollow.Id);

        if (existingFollow != null)
        {
            return BadRequest();
        }

        var follow = new Follow
        {
            FollowerId = currentUserId,
            FollowingId = userToFollow.Id,
            CreatedAt = DateTime.Now
        };

        _db.Follows.Add(follow);
        await _db.SaveChangesAsync();

        return RedirectToAction("Index", "Profile", new { username });
    }

    [HttpPost]
    public async Task<IActionResult> Unfollow(string username)
    {
        var currentUserId = _userManager.GetUserId(User);
        var userToUnfollow = await _userManager.FindByNameAsync(username);

        if (userToUnfollow == null || userToUnfollow.Id == currentUserId)
        {
            return BadRequest();
        }

        var existingFollow = await _db.Follows
            .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowingId == userToUnfollow.Id);

        if (existingFollow == null)
        {
            return BadRequest();
        }

        _db.Follows.Remove(existingFollow);
        await _db.SaveChangesAsync();

        return RedirectToAction("Index", "Profile", new { username });
    }
}