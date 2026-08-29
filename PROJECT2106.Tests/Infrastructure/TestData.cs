using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PROJECT2106.Models;

namespace PROJECT2106.Tests.Infrastructure;

public static class TestData
{
    public static async Task<AppUser> CreateUserAsync(
        IServiceProvider services,
        string username,
        string? role = "User")
    {
        var userManager =
            services.GetRequiredService<
                UserManager<AppUser>>();

        var roleManager =
            services.GetRequiredService<
                RoleManager<IdentityRole>>();

        if (role != null &&
            !await roleManager.RoleExistsAsync(role))
        {
            var roleResult =
                await roleManager.CreateAsync(
                    new IdentityRole(role));

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(
                        "; ",
                        roleResult.Errors.Select(
                            e => e.Description)));
            }
        }

        var user = new AppUser
        {
            UserName = username,
            Email = $"{username}@test.local",
            CreatedAt = DateTime.Now
        };

        var result =
            await userManager.CreateAsync(
                user,
                "Test123!");

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join(
                    "; ",
                    result.Errors.Select(
                        e => e.Description)));
        }

        if (role != null)
        {
            var roleResult =
                await userManager.AddToRoleAsync(
                    user,
                    role);

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(
                        "; ",
                        roleResult.Errors.Select(
                            e => e.Description)));
            }
        }

        return user;
    }

    public static HttpClient CreateAuthenticatedClient(
        ProjectWebApplicationFactory factory,
        AppUser user,
        string role = "User",
        bool allowRedirects = false)
    {
        var client =
            factory.CreateClient(
                new()
                {
                    AllowAutoRedirect =
                        allowRedirects
                });

        client.DefaultRequestHeaders.Add(
            "X-Test-UserId",
            user.Id);

        client.DefaultRequestHeaders.Add(
            "X-Test-Username",
            user.UserName!);

        client.DefaultRequestHeaders.Add(
            "X-Test-Role",
            role);

        return client;
    }
}
