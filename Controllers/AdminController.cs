using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECT2106.Data;
using PROJECT2106.Models;

namespace PROJECT2106.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public AdminController(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        const int pageSize = 25;
        page = Math.Max(page, 1);

        var userQuery = _userManager.Users
            .AsNoTracking();

        var userCount = await userQuery.CountAsync();

        var users = await userQuery
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var model = new ViewModels.AdminDashboardViewModel
        {
            UserCount = userCount,
            PostCount = await _db.Posts
                .AsNoTracking()
                .CountAsync(),

            CommentCount = await _db.Comments
                .AsNoTracking()
                .CountAsync(),

            Users = new ViewModels.PagedResult<AppUser>
            {
                Items = users,
                Page = page,
                PageSize = pageSize,
                TotalItems = userCount
            }
        };

        return View(model);
    }
}
