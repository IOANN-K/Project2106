using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PROJECT2106.Tests.Infrastructure;

public sealed class TestAuthHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult>
        HandleAuthenticateAsync()
    {
        var userId =
            Request.Headers["X-Test-UserId"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(
                AuthenticateResult.NoResult());
        }

        var username =
            Request.Headers["X-Test-Username"]
                .FirstOrDefault()
            ?? "test-user";

        var role =
            Request.Headers["X-Test-Role"]
                .FirstOrDefault();

        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                userId),

            new(
                ClaimTypes.Name,
                username)
        };

        if (!string.IsNullOrWhiteSpace(role))
        {
            claims.Add(
                new Claim(
                    ClaimTypes.Role,
                    role));
        }

        var identity = new ClaimsIdentity(
            claims,
            AuthenticationScheme);

        var principal =
            new ClaimsPrincipal(identity);

        var ticket =
            new AuthenticationTicket(
                principal,
                AuthenticationScheme);

        return Task.FromResult(
            AuthenticateResult.Success(ticket));
    }
}
