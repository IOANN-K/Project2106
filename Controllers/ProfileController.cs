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
        if (string.IsNullOrWhiteSpace(username))
            return NotFound();

        var user = await _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == username);

        if (user == null)
            return NotFound();

        var contributions = await _db.Posts
            .AsNoTracking()
            .Where(p => p.AuthorId == user.Id)
            .Include(p => p.Place)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var createdPlaces = await _db.Places
            .AsNoTracking()
            .Where(p => p.CreatedByUserId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
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

        var contributionsCount = contributions.Count;
        var createdPlacesCount = createdPlaces.Count;

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
