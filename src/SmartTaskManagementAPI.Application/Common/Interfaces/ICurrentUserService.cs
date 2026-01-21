using System;
using System.Security.Claims;

namespace SmartTaskManagementAPI.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserEmail { get; }
    ClaimsPrincipal? User { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    IEnumerable<Claim>? GetClaims();
}
