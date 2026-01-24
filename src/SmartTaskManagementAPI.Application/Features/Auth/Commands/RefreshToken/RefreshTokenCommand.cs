using System;
using MediatR;
using SmartTaskManagementAPI.Application.Features.Auth.DTOs;

namespace SmartTaskManagementAPI.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommand : IRequest<AuthResponse>
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
