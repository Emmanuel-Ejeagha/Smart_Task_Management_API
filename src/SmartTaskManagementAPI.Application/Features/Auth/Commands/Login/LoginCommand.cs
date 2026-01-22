using System;
using MediatR;
using SmartTaskManagementAPI.Application.Features.Auth.DTOs;

namespace SmartTaskManagementAPI.Application.Features.Auth.Commands.Login;

public class LoginCommand : IRequest<AuthResponse>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

