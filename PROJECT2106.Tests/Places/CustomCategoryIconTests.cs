using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using PROJECT2106.Controllers;
using PROJECT2106.Data;
using PROJECT2106.Models;
using PROJECT2106.Tests.Infrastructure;
using PROJECT2106.ViewModels;

namespace PROJECT2106.Tests.Places;

public sealed class CustomCategoryIconTests : IntegrationTestBase
{
    public CustomCategoryIconTests(PostgresFixture postgres) : base(postgres)
    {
    }

    [Fact]
    public async Task Valid_Custom_Category_Icon_Path_Is_Persisted()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var user = await TestData.CreateUserAsync(services, "category-icon-owner");
        using var environment = new TemporaryWebHostEnvironment();
        var controller = CreateController(services, user, environment);

        var result = await controller.Create(new CustomCategoryCreateViewModel
        {
            Name = "River access",
            Icon = CreateFile("marker.png", "image/png")
        });

        Assert.IsType<RedirectToActionResult>(result);
        var category = services.GetRequiredService<AppDbContext>().CustomCategories.Single();
        Assert.Matches("^/uploads/category-icons/[a-f0-9]{32}\\.png$", category.IconPath!);

        var fileName = Path.GetFileName(category.IconPath)!;
        Assert.True(File.Exists(Path.Combine(environment.WebRootPath, "uploads", "category-icons", fileName)));
    }

    [Fact]
    public async Task Invalid_Custom_Category_Icon_Is_Rejected()
    {
        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var user = await TestData.CreateUserAsync(services, "category-icon-invalid");
        using var environment = new TemporaryWebHostEnvironment();
        var controller = CreateController(services, user, environment);

        var result = await controller.Create(new CustomCategoryCreateViewModel
        {
            Name = "Unsafe icon",
            Icon = CreateFile("marker.svg", "image/svg+xml")
        });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Empty(services.GetRequiredService<AppDbContext>().CustomCategories);
    }

    private static CustomCategoryController CreateController(
        IServiceProvider services,
        AppUser user,
        IWebHostEnvironment environment)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName!)
            },
            "Test"));

        return new CustomCategoryController(
            services.GetRequiredService<AppDbContext>(),
            services.GetRequiredService<UserManager<AppUser>>(),
            environment)
        {
            ControllerContext = new ControllerContext(new ActionContext(
                new DefaultHttpContext
                {
                    User = principal,
                    RequestServices = services
                },
                new RouteData(),
                new ControllerActionDescriptor()))
        };
    }

    private static IFormFile CreateFile(string fileName, string contentType)
    {
        return new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "Icon", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class TemporaryWebHostEnvironment : IWebHostEnvironment, IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), $"project2106-tests-{Guid.NewGuid():N}");

        public TemporaryWebHostEnvironment()
        {
            Directory.CreateDirectory(_root);
            WebRootPath = Path.Combine(_root, "wwwroot");
            Directory.CreateDirectory(WebRootPath);
            ContentRootPath = _root;
            WebRootFileProvider = new PhysicalFileProvider(WebRootPath);
            ContentRootFileProvider = new PhysicalFileProvider(ContentRootPath);
        }

        public string ApplicationName { get; set; } = "PROJECT2106.Tests";
        public IFileProvider WebRootFileProvider { get; set; }
        public string WebRootPath { get; set; }
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }

        public void Dispose()
        {
            (WebRootFileProvider as IDisposable)?.Dispose();
            (ContentRootFileProvider as IDisposable)?.Dispose();
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
    }
}
