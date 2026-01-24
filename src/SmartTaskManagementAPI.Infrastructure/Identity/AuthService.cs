using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SmartTaskManagementAPI.Application.Common.Interfaces;
using SmartTaskManagementAPI.Application.Features.Auth.DTOs;
using SmartTaskManagementAPI.Domain.Entities;
using SmartTaskManagementAPI.Domain.ValueObjects;
using SmartTaskManagementAPI.Infrastructure.Data;

namespace SmartTaskManagementAPI.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ITokenService tokenService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _configuration = configuration;
        _logger = logger;
    }
    
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Email.IsValid(request.Email))
                throw new ArgumentException("Invalid email format");

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
                throw new InvalidOperationException("User with this email already exists");

            var tenantSlug = GenerateSlug(request.TenantName);

            var existingTenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Slug == tenantSlug, cancellationToken);

            if (existingTenant != null)
                throw new InvalidOperationException("Tenant with this name already exists");

            var tenant = Tenant.Create(
                request.TenantName,
                tenantSlug,
                Guid.Empty);

            await _context.Tenants.AddAsync(tenant, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var domainUser = User.Create(
                request.FirstName,
                request.LastName,
                request.Email,
                string.Empty, // Password will be set in Identity user
                tenant.Id,
                Guid.Empty,
                "Admin");

            await _context.Users.AddAsync(domainUser, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // Create identity user
            var identityUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                TenantId = tenant.Id,
                DomainUserId = domainUser.Id,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(identityUser, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to crete user: {errors}");
            }

            // Assign admin role to the first user
            await EnsureRolesExist();
            await _userManager.AddToRoleAsync(identityUser, "Admin");

            // Update domain user with the identity user Id
            domainUser.MarkAsUpdated(domainUser.Id); // self-update

            await _context.SaveChangesAsync(cancellationToken);

            // Generate tokens
            var authResponse = await GenerateTokenAsync(identityUser);
            return authResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email: {Email}", request.Email);
            throw;
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // find user by email
            var identityUser = await _userManager.FindByEmailAsync(request.Email);
            if (identityUser == null)
                throw new UnauthorizedAccessException("Invalid credentials");

            // check if user is active
            if (!identityUser.IsActive)
                throw new UnauthorizedAccessException("Account is deactivated");

            // Verify password
            var isValidPassword = await _userManager.CheckPasswordAsync(identityUser, request.Password);
            if (!isValidPassword)
                throw new UnauthorizedAccessException("Invalid credentials");

            // Get domain user
            var domainUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == identityUser.DomainUserId && !u.IsDeleted, cancellationToken);

            if (domainUser == null)
                throw new InvalidOperationException("Associated domain user not found");

            // Update domain user's last login (if we has that propety)
            domainUser.UpdateLastLogin();

            // Generate tokens
            var authResponse = await GenerateTokenAsync(identityUser);
            return authResponse;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
            throw;
        }
    }

    public async Task<AuthResponse> RefreshTokenAsync(string accessToken, string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate expired access token]
            var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
            if (principal?.Identity?.Name == null)
                throw new SecurityTokenException("Invalid token");

            // Get user from token 
            var userEmail = principal.Identity.Name;
            var identityUser = await _userManager.FindByEmailAsync(userEmail);

            if (identityUser == null || !identityUser.IsActive)
                throw new UnauthorizedAccessException("Invalid token");

            // Validate refresh token
            if (identityUser.RefreshToken != refreshToken || identityUser.RefreshTokenExpireTime <= DateTime.UtcNow)
                throw new SecurityTokenException("Invalid referesh token");

            // Generate new tokens
            var authResponse = await GenerateTokenAsync(identityUser);
            return authResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            throw;
        }
    }

    public async Task<bool> RevokeRefreshTokenAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
                return false;

            var identityUser = await _userManager.FindByIdAsync(userId);
            if (identityUser == null)
                return false;

            identityUser.RefreshToken = string.Empty;
            identityUser.RefreshTokenExpireTime = DateTime.MinValue;

            await _userManager.UpdateAsync(identityUser);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token for user: {UserId}", userId);
            return false;
        }
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenhandler = new JwtSecurityTokenHandler();
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            tokenhandler.ValidateToken(token, validationParameters, out _);
            return System.Threading.Tasks.Task.FromResult(true);
        }
        catch (System.Exception)
        {

            return System.Threading.Tasks.Task.FromResult(false);
        }
    }

    private async Task<AuthResponse> GenerateTokenAsync(ApplicationUser identityUser)
    {
        // Get user roles
        var roles = await _userManager.GetRolesAsync(identityUser);

        // Create claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, identityUser.Id.ToString()),
            new Claim(ClaimTypes.Email, identityUser.Email!),
            new Claim(ClaimTypes.Name, identityUser.GetFullName()),
            new Claim("TenantId", identityUser.TenantId.ToString()),
            new Claim("DomainUserId", identityUser.DomainUserId?.ToString() ?? string.Empty)
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Generate access token
        var accessToken = _tokenService.GenerateAccessToken(claims);

        // Generate refresh token
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Get token expiration
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var expiresAt = DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["AccessTokenExpirationMinutes"] ?? "15"));

        // Update user with new refresh token
        identityUser.RefreshToken = refreshToken;
        identityUser.RefreshTokenExpireTime = DateTime.UtcNow.AddDays(Convert.ToDouble(jwtSettings["RefereshTokenExpirationDays"] ?? "7"));

        await _userManager.UpdateAsync(identityUser);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiredAt = expiresAt,
            UserId = identityUser.Email!,
            FullName = identityUser.GetFullName(),
            Role = roles.FirstOrDefault() ?? "User",
            TenantId = identityUser.TenantId
        };
    }

    private async System.Threading.Tasks.Task EnsureRolesExist()
    {
        var roles = new[] { "Admin", "User" };

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new ApplicationRole(role));
            }
        }
    }

    private string GenerateSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be empty");

        var slug = input.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("--", "-")
            .Trim('-');

        // Remove any non-alphanummeric characters except hyphen
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");

        // Remove multiple hyphens
        slug = Regex.Replace(slug, @"-+", "-");

        return slug;
    }
}
