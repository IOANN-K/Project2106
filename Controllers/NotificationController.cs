using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECT2106.Data;
using PROJECT2106.Models;

namespace PROJECT2106.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public NotificationController(
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

        var notifications = await _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .Include(n => n.ActorUser)
            .Include(n => n.Post)
            .Include(n => n.Place)
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .ToListAsync();

        return View(notifications);
    }

    [HttpPost]
    public async Task<IActionResult> Read(int id)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Forbid();

        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n =>
                n.Id == id &&
                n.UserId == userId);

        if (notification == null)
            return NotFound();

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.Now;

            await _db.SaveChangesAsync();
        }

        return RedirectToTarget(notification);
    }

    [HttpPost]
    public async Task<IActionResult> ReadAll()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Forbid();

        var now = DateTime.Now;

        await _db.Notifications
            .Where(n =>
                n.UserId == userId &&
                !n.IsRead)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now));

        return RedirectToAction(nameof(Index));
    }

    private IActionResult RedirectToTarget(Notification notification)
    {
        return notification.Type switch
        {
            NotificationType.PostLiked
                or NotificationType.PostCommented
                or NotificationType.FollowedUserPosted
                    when notification.PostId.HasValue
                => RedirectToAction(
                    "Details",
                    "Post",
                    new { id = notification.PostId.Value }),

            NotificationType.Followed
                when notification.ActorUser?.UserName != null
                => RedirectToAction(
                    "Index",
                    "Profile",
                    new
                    {
                        username =
                            notification.ActorUser.UserName
                    }),

            _ when notification.PlaceId.HasValue
                => RedirectToAction(
                    "Details",
                    "Place",
                    new { id = notification.PlaceId.Value }),

            _ => RedirectToAction(nameof(Index))
        };
    }
}
