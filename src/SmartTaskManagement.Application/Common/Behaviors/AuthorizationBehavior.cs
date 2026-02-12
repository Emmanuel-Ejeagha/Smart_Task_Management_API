using MediatR;
using Microsoft.Extensions.Logging;
using SmartTaskManagement.Application.Common.Interfaces;

namespace SmartTaskManagement.Application.Common.Behaviors;

/// <summary>
/// Authorization behavior for MediatR pipeline
/// Checks user authorization before executing requests
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuthorizationBehavior<TRequest, TResponse>> _logger;

    public AuthorizationBehavior(
        ICurrentUserService currentUserService,
        ILogger<AuthorizationBehavior<TRequest, TResponse>> logger)
    {
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Check if request requires authorization
        var authorizeAttributes = request.GetType()
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .ToList();

        if (!authorizeAttributes.Any())
            return await next();

        // Check if user is authenticated
        if (!_currentUserService.IsAuthenticated)
        {
            _logger.LogWarning(
                "Unauthenticated user attempted to access {RequestName}",
                typeof(TRequest).Name);
            
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        // Check roles
        var requiredRoles = authorizeAttributes
            .SelectMany(a => a.Roles ?? Array.Empty<string>())
            .ToList();

        if (requiredRoles.Any())
        {
            var hasRequiredRole = requiredRoles.Any(role => _currentUserService.IsInRole(role));
            
            if (!hasRequiredRole)
            {
                _logger.LogWarning(
                    "User {UserId} with roles {UserRoles} attempted to access {RequestName} requiring roles {RequiredRoles}",
                    _currentUserService.UserId,
                    _currentUserService.Roles,
                    typeof(TRequest).Name,
                    requiredRoles);
                
                throw new UnauthorizedAccessException("User does not have required role");
            }
        }

        // Check policies
        var requiredPolicies = authorizeAttributes
            .Select(a => a.Policy)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        foreach (var policy in requiredPolicies)
        {
            // Implement policy checks here
            // This would typically call an IPolicyService
            throw new NotImplementedException("Policy-based authorization not implemented");
        }

        return await next();
    }
}

/// <summary>
/// Authorization attribute for MediatR requests
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class AuthorizeAttribute : Attribute
{
    public string[]? Roles { get; set; }
    public string? Policy { get; set; }

    public AuthorizeAttribute() { }

    public AuthorizeAttribute(string role)
    {
        Roles = new[] { role };
    }

    public AuthorizeAttribute(params string[] roles)
    {
        Roles = roles;
    }
}