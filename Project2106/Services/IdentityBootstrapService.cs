using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using PROJECT2106.Models;
using PROJECT2106.Options;

namespace PROJECT2106.Services;

public sealed class IdentityBootstrapService
{
    private static readonly string[] RequiredRoles = ["Admin", "User"];

    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly AdminBootstrapOptions _options;
    private readonly ILogger<IdentityBootstrapService> _logger;

    public IdentityBootstrapService(
        RoleManager<IdentityRole> roleManager,
        UserManager<AppUser> userManager,
        IOptions<AdminBootstrapOptions> options,
        ILogger<IdentityBootstrapService> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        await EnsureRolesAsync();

        if (!_options.Enabled)
        {
            _logger.LogInformation("Administrator bootstrap is disabled.");
            return;
        }

        ValidateAdminConfiguration();

        var existingAdmin = await _userManager.FindByEmailAsync(_options.Email);

        if (existingAdmin is not null)
        {
            if (!await _userManager.IsInRoleAsync(existingAdmin, "Admin"))
            {
                var existingRoleResult = await _userManager.AddToRoleAsync(existingAdmin, "Admin");

                if (!existingRoleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Failed to assign the Admin role to the existing bootstrap account: " +
                        FormatErrors(existingRoleResult));
                }
            }

            _logger.LogInformation(
                "Bootstrap administrator already exists. Password and account data were not changed.");

            return;
        }

        var admin = new AppUser
        {
            UserName = _options.UserName,
            Email = _options.Email,
            CreatedAt = DateTime.Now
        };

        var createResult = await _userManager.CreateAsync(admin, _options.Password);

        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create bootstrap administrator: {FormatErrors(createResult)}");
        }

        var addRoleResult = await _userManager.AddToRoleAsync(admin, "Admin");

        if (addRoleResult.Succeeded)
        {
            _logger.LogInformation(
                "Bootstrap administrator {Email} created successfully.",
                _options.Email);

            return;
        }

        // Do not leave a newly-created bootstrap user in a partial state.
        var rollbackResult = await _userManager.DeleteAsync(admin);

        if (!rollbackResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Failed to assign Admin role to bootstrap administrator, and rollback also failed. " +
                $"Role errors: {FormatErrors(addRoleResult)}. " +
                $"Rollback errors: {FormatErrors(rollbackResult)}");
        }

        throw new InvalidOperationException(
            $"Failed to assign Admin role to bootstrap administrator: {FormatErrors(addRoleResult)}");
    }

    private async Task EnsureRolesAsync()
    {
        foreach (var roleName in RequiredRoles)
        {
            if (await _roleManager.RoleExistsAsync(roleName))
                continue;

            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create role '{roleName}': {FormatErrors(result)}");
            }
        }
    }

    private void ValidateAdminConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.Email))
            throw new InvalidOperationException(
                "BootstrapAdmin:Email must be configured when administrator bootstrap is enabled.");

        if (string.IsNullOrWhiteSpace(_options.UserName))
            throw new InvalidOperationException(
                "BootstrapAdmin:UserName must be configured when administrator bootstrap is enabled.");

        if (string.IsNullOrWhiteSpace(_options.Password))
            throw new InvalidOperationException(
                "BootstrapAdmin:Password must be configured when administrator bootstrap is enabled.");
    }

    private static string FormatErrors(IdentityResult result)
    {
        return string.Join(
            "; ",
            result.Errors.Select(error => $"{error.Code}: {error.Description}"));
    }
}
