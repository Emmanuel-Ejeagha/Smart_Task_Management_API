using System;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartTaskManagementAPI.Application.Common.Interfaces;
using SmartTaskManagementAPI.Application.Features.Auth.DTOs;

namespace SmartTaskManagementAPI.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IAuthService _authService;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IAuthService authService,
        ILogger<RegisterCommandHandler> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var registerRegister = new RegisterRequest
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Password = request.Password,
                TenantName = request.TenantName
            };

            var result = await _authService.RegisterAsync(registerRegister, cancellationToken);
            _logger.LogInformation("User registered succesfully: {Email}", request.Email);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occured while registering user: {Email}", request.Email);
            throw;
        }
    }
}
