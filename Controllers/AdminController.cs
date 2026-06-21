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

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.ToListAsync();
        var posts = await _db.Posts.CountAsync();
        var comments = await _db.Comments.CountAsync();

        ViewBag.UserCount = users.Count;
        ViewBag.PostCount = posts;
        ViewBag.CommentCount = comments;
        ViewBag.Users = users;

        return View();
    }
}