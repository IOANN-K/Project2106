using Microsoft.AspNetCore.Mvc;
using PROJECT2106.Models;

namespace PROJECT2106.Controllers;

public class ProfileController : Controller
{
    public IActionResult Index(string username)
    {
        // Тимчасово — потім з БД
        var user = new AppUser
        {
            UserName = username ?? "test_user",
            Bio = "This is a test profile"
        };

        return View(user);
    }
}