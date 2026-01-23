using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SmartTaskManagementAPI.Application.Common.Interfaces;

namespace SmartTaskManagementAPI.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? UserEmail => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

    public ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role)
    {
        return User?.IsInRole(role) ?? false;
    }

    public IEnumerable<Claim>? GetClaims()
    {
        return User?.Claims;
    }

}
