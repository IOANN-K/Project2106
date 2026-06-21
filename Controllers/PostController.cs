using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECT2106.Data;
using PROJECT2106.Models;

namespace PROJECT2106.Controllers;

public class PostController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<PostController> _logger;

    public PostController(AppDbContext db, ILogger<PostController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // GET /Post
    public async Task<IActionResult> Index()
    {
        var posts = await _db.Posts
            .Include(p => p.Author)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return View(posts);
    }

    // GET /Post/Details/1
    public async Task<IActionResult> Details(int id)
    {
        var post = await _db.Posts
            .Include(p => p.Author)
            .Include(p => p.Comments)
            .ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null) return NotFound();
        return View(post);
    }

    // GET /Post/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST /Post/Create
    [HttpPost]
    public async Task<IActionResult> Create(Post post)
    {
        if (ModelState.IsValid)
        {
            post.CreatedAt = DateTime.Now;
            _db.Posts.Add(post);
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
        return View(post);
    }

    // POST /Post/Edit/1
    [HttpPost]
    public async Task<IActionResult> Edit(int id, Post post)
    {
        if (id != post.Id) return NotFound();

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
        return View(post);
    }

    // POST /Post/Delete/1
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post != null)
        {
            _db.Posts.Remove(post);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Deleted post #{Id}", id);
        }
        return RedirectToAction(nameof(Index));
    }
}