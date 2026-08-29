using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PROJECT2106.Data;

namespace PROJECT2106.Tests.Infrastructure;

public sealed class ProjectWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public ProjectWebApplicationFactory(
        string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            _connectionString);

        builder.UseSetting(
            "BootstrapAdmin:Enabled",
            "false");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<
                DbContextOptions<AppDbContext>>();

            services.RemoveAll<AppDbContext>();

            services.AddDbContext<AppDbContext>(
                options =>
                    options.UseNpgsql(
                        _connectionString));

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme =
                        TestAuthHandler.AuthenticationScheme;

                    options.DefaultChallengeScheme =
                        TestAuthHandler.AuthenticationScheme;
                })
                .AddScheme<
                    AuthenticationSchemeOptions,
                    TestAuthHandler>(
                    TestAuthHandler.AuthenticationScheme,
                    _ => { });
        });
    }
}
