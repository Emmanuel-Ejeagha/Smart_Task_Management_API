using System;

namespace SmartTaskManagementAPI.Application.Features.Auth.DTOs;

public class RegisterRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
}
