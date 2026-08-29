using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PROJECT2106.Controllers;
using PROJECT2106.Data;
using PROJECT2106.Models;
using PROJECT2106.Tests.Infrastructure;
using PROJECT2106.ViewModels;

namespace PROJECT2106.Tests.Profiles;

public sealed class ProfileSettingsTests : IntegrationTestBase
{
    public ProfileSettingsTests(PostgresFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task Authenticated_User_Can_Change_Biography()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var user = await TestData.CreateUserAsync(services, "profile-editor");
        var controller = CreateProfileController(services, user);

        var result = await controller.Edit(new EditProfileViewModel
        {
            Bio = "  Mountain researcher\nField photographer  "
        });

        Assert.IsType<RedirectToActionResult>(result);

        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var updated = await userManager.FindByIdAsync(user.Id);
        Assert.Equal("Mountain researcher\nField photographer", updated!.Bio);
    }

    [Fact]
    public async Task Invalid_Avatar_File_Is_Rejected()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var user = await TestData.CreateUserAsync(services, "invalid-avatar");
        var controller = CreateProfileController(services, user);
        var avatar = CreateFile("avatar.exe", "application/octet-stream");

        var result = await controller.Edit(new EditProfileViewModel
        {
            Bio = "Explorer",
            Avatar = avatar
        });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(EditProfileViewModel.Avatar)));
    }

    [Fact]
    public async Task Correct_Current_Password_Changes_Password()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var user = await TestData.CreateUserAsync(services, "password-change");
        var controller = CreateAccountController(services, user);

        var result = await controller.ChangePassword(new ChangePasswordViewModel
        {
            CurrentPassword = "Test123!",
            NewPassword = "Changed123!",
            ConfirmPassword = "Changed123!"
        });

        Assert.IsType<RedirectToActionResult>(result);
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        Assert.True(await userManager.CheckPasswordAsync(user, "Changed123!"));
    }

    [Fact]
    public async Task Incorrect_Current_Password_Is_Rejected()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var user = await TestData.CreateUserAsync(services, "password-reject");
        var controller = CreateAccountController(services, user);

        var result = await controller.ChangePassword(new ChangePasswordViewModel
        {
            CurrentPassword = "Wrong123!",
            NewPassword = "Changed123!",
            ConfirmPassword = "Changed123!"
        });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        Assert.True(await userManager.CheckPasswordAsync(user, "Test123!"));
    }

    private static ProfileController CreateProfileController(IServiceProvider services, AppUser user)
    {
        return new ProfileController(
            services.GetRequiredService<UserManager<AppUser>>(),
            services.GetRequiredService<AppDbContext>())
        {
            ControllerContext = CreateControllerContext(services, user)
        };
    }

    private static AccountController CreateAccountController(IServiceProvider services, AppUser user)
    {
        var controllerContext = CreateControllerContext(services, user);
        services.GetRequiredService<IHttpContextAccessor>().HttpContext = controllerContext.HttpContext;

        return new AccountController(
            services.GetRequiredService<UserManager<AppUser>>(),
            services.GetRequiredService<SignInManager<AppUser>>(),
            services.GetRequiredService<ILogger<AccountController>>())
        {
            ControllerContext = controllerContext
        };
    }

    private static ControllerContext CreateControllerContext(IServiceProvider services, AppUser user)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName!)
            },
            "Test"));

        var httpContext = new DefaultHttpContext
        {
            User = principal,
            RequestServices = services
        };

        return new ControllerContext(new ActionContext(
            httpContext,
            new RouteData(),
            new ControllerActionDescriptor()));
    }

    private static IFormFile CreateFile(string fileName, string contentType)
    {
        return new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "Avatar", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
