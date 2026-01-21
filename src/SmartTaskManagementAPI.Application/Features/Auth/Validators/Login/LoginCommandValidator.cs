using System;
using FluentValidation;
using SmartTaskManagementAPI.Application.Features.Auth.Commands.Login;

namespace SmartTaskManagementAPI.Application.Features.Auth.Validators.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email address.");

        RuleFor(p => p.Password)
            .NotEmpty().WithMessage("Password is required");

            
    }
}
