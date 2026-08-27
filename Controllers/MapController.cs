using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECT2106.Data;
using PROJECT2106.ViewModels;

namespace PROJECT2106.Controllers;

public class MapController : Controller
{
    private readonly AppDbContext _db;

    public MapController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Markers()
    {
        var places = await _db.Places
            .AsNoTracking()
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Latitude,
                p.Longitude,
                p.SystemCategory,
                CustomCategoryName = p.CustomCategory != null
                    ? p.CustomCategory.Name
                    : null
            })
            .ToListAsync();

        var markers = places
            .Select(p => new PlaceMarkerViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Latitude = p.Latitude,
                Longitude = p.Longitude,

                Category = p.SystemCategory.HasValue
                    ? p.SystemCategory.Value.ToString()
                    : p.CustomCategoryName ?? "Other",

                // Place rating persistence will be connected later.
                Rating = null
            })
            .ToList();

        return Json(markers);
    }
}
