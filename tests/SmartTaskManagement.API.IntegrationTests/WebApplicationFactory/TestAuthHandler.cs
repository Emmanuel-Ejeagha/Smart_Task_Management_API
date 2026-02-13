using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SmartTaskManagement.API.IntegrationTests.WebApplicationFactory;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "TestScheme";
    public const string UserId = "test-user-id";
    public const string TenantId = "00000000-0000-0000-0000-000000000001";
    public const string UserRole = "User";
    public const string AdminRole = "Admin";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock) : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, UserId),
            new(ClaimTypes.Email, "test@example.com"),
            new("tenantId", TenantId),
            new(ClaimTypes.Role, UserRole)
        };

        // If the request header contains "Admin=true", add Admin role
        if (Context.Request.Headers.TryGetValue("Admin", out var adminValue) && adminValue == "true")
        {
            claims.Add(new Claim(ClaimTypes.Role, AdminRole));
        }

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}