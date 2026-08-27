using Microsoft.EntityFrameworkCore;
using PROJECT2106.Data;
using PROJECT2106.Models;
using Microsoft.AspNetCore.Identity;
using PROJECT2106.Options;
using PROJECT2106.Services;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
var supportedCultures = new[]
    {
        new CultureInfo("en-US")
    };

    builder.Services.Configure<RequestLocalizationOptions>(options =>
    {
        options.DefaultRequestCulture =
            new Microsoft.AspNetCore.Localization.RequestCulture("en-US");

        options.SupportedCultures = supportedCultures;
        options.SupportedUICultures = supportedCultures;
    });
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;
});

builder.Services.Configure<AdminBootstrapOptions>(
    builder.Configuration.GetSection(AdminBootstrapOptions.SectionName));

builder.Services.AddScoped<IdentityBootstrapService>();

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddSingleton<PROJECT2106.Services.IActivityLogService, 
                               PROJECT2106.Services.ActivityLogService>();

var app = builder.Build();
app.UseRequestLocalization();
app.UseMiddleware<PROJECT2106.Middleware.RequestLoggingMiddleware>();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();








if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();

    var bootstrapService =
        scope.ServiceProvider.GetRequiredService<IdentityBootstrapService>();

    await bootstrapService.InitializeAsync();
}

app.Run();

public partial class Program
{
}
