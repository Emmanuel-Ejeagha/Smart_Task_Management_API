using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SmartTaskManagement.Infrastructure.Authentication;

/// <summary>
/// Custom JWT bearer events handler
/// </summary>
public class JwtBearerEventsHandler : JwtBearerEvents
{
    private readonly ILogger<JwtBearerEventsHandler> _logger;
    private readonly Auth0ClaimsPrincipalFactory _claimsPrincipalFactory;

    public JwtBearerEventsHandler(
        ILogger<JwtBearerEventsHandler> logger,
        Auth0ClaimsPrincipalFactory claimsPrincipalFactory)
    {
        _logger = logger;
        _claimsPrincipalFactory = claimsPrincipalFactory;
        OnTokenValidated = OnTokenValidatedHandler;
        OnAuthenticationFailed = OnAuthenticationFailedHandler;
        OnChallenge = OnChallengeHandler;
    }

    private async Task OnTokenValidatedHandler(TokenValidatedContext context)
    {
        try
        {
            // Transform claims after token validation
            var transformedPrincipal = _claimsPrincipalFactory.Transform(context.Principal!);
            context.Principal = transformedPrincipal;

            // Log successful authentication
            var userId = context.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} authenticated successfully", userId);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token validation");
            context.Fail(ex);
        }
    }

    private async Task OnAuthenticationFailedHandler(AuthenticationFailedContext context)
    {
        _logger.LogError(context.Exception, "JWT authentication failed");
        await Task.CompletedTask;
    }

    private async Task OnChallengeHandler(JwtBearerChallengeContext context)
    {
        _logger.LogWarning("JWT authentication challenge: {Error}, {ErrorDescription}, {ErrorUri}",
            context.Error, context.ErrorDescription, context.ErrorUri);
        
        // Customize challenge response
        context.HandleResponse();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            error = context.Error,
            error_description = context.ErrorDescription,
            error_uri = context.ErrorUri
        };
        
        await context.Response.WriteAsJsonAsync(response);
    }
}