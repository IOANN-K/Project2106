using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PROJECT2106.Models;
using PROJECT2106.ViewModels;

namespace PROJECT2106.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    // GET /Account/Register
    public IActionResult Register() => View();

    // POST /Account/Register
    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new AppUser
        {
            UserName = model.UserName,
            Email = model.Email,
            CreatedAt = DateTime.Now
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "User");
            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("New user: {UserName}", user.UserName);
            return RedirectToAction("Index", "Post");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    // GET /Account/Login
    public IActionResult Login() => View();

    // POST /Account/Login
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // Шукаємо користувача по email
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Wrong email or password");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            _logger.LogInformation("SignIn: {Email}", model.Email);
            return RedirectToAction("Index", "Post");
        }

        ModelState.AddModelError(string.Empty, "Wrong email or password");
        return View(model);
    }
    // POST /Account/Logout
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Post");
    }
}