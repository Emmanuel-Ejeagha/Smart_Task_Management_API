using System;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartTaskManagementAPI.Application.Common.Interfaces;
using SmartTaskManagementAPI.Application.Features.Auth.DTOs;

namespace SmartTaskManagementAPI.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IAuthService _authService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(IAuthService authService, ILogger<RefreshTokenCommandHandler> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(
                request.AccessToken,
                request.RefreshToken,
                cancellationToken);

            _logger.LogInformation("Token refreshed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occured while refreshing token");
            throw;
        }
    }
}
