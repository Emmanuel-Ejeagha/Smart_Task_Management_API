using System;
using MediatR;

namespace SmartTaskManagementAPI.Application.Features.Auth.DTOs;

public class RegisterCommand : IRequest<AuthResponse>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
}
