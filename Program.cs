using Microsoft.EntityFrameworkCore;
using PROJECT2106.Data;
using PROJECT2106.Data.Seed;
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
builder.Services.Configure<SmtpOptions>(
    builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.Configure<DemoSeedOptions>(
    builder.Configuration.GetSection(DemoSeedOptions.SectionName));

builder.Services.AddScoped<IdentityBootstrapService>();
builder.Services.AddScoped<DemoDataSeeder>();
builder.Services.AddScoped<IPasswordResetEmailSender, SmtpPasswordResetEmailSender>();
builder.Services.AddHealthChecks();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

var app = builder.Build();
app.UseRequestLocalization();
app.UseMiddleware<PROJECT2106.Middleware.RequestLoggingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapHealthChecks("/health");

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

    if (app.Environment.IsDevelopment())
    {
        var demoOptions = scope.ServiceProvider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<DemoSeedOptions>>()
            .Value;

        if (demoOptions.Enabled)
        {
            var demoSeeder = scope.ServiceProvider
                .GetRequiredService<DemoDataSeeder>();

            await demoSeeder.SeedAsync();
        }
    }
}

app.Run();

public partial class Program
{
}
