using System;
using Microsoft.AspNetCore.Identity;
using SmartTaskManagementAPI.Application.Common.Interfaces;

namespace SmartTaskManagementAPI.Infrastructure.Identity;

public class PasswordHashService : IPasswordHashService
{
    private readonly PasswordHasher<ApplicationUser> _passwordHasher;

    public PasswordHashService()
    {
        _passwordHasher = new PasswordHasher<ApplicationUser>();
    }

    public string HashPassword(string password)
    {
        var user = new ApplicationUser();
        return _passwordHasher.HashPassword(user, password);
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        var user = new ApplicationUser { PasswordHash = hashedPassword };
        var result = _passwordHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);

        return result == PasswordVerificationResult.Success;
    }
}
