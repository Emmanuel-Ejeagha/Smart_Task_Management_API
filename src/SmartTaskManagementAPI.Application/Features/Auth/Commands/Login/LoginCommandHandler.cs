using System;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartTaskManagementAPI.Application.Common.Interfaces;
using SmartTaskManagementAPI.Application.Features.Auth.DTOs;

namespace SmartTaskManagementAPI.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IAuthService _authService;
    private readonly ILogger<LoginCommandHandler> _logger;
    public LoginCommandHandler(
        IAuthService authService,
        ILogger<LoginCommandHandler> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var loginRequest = new LoginRequest
            {
                Email = request.Email,
                Password = request.Password
            };

            var result = await _authService.LoginAsync(loginRequest, cancellationToken);
            _logger.LogInformation("User logged in successfully: {Email}", request.Email);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occured while logging in user: {Email}", request.Email);
            throw;
        }
    }
}
