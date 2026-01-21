using FluentValidation;
using SmartTaskManagementAPI.Application.Features.Auth.DTOs;

namespace SmartTaskManagementAPI.Application.Features.Auth.Validator.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(50).WithMessage("First name must not exceed 50 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(50).WithMessage("Last name must not exceed 50 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email address.")
            .MaximumLength(100).WithMessage("Email must not exceed 100 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .MaximumLength(50).WithMessage("Password must not exceed 50 characters")
            .Matches(@"[A-Z]+").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]+").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]+").WithMessage("Password must contain at least one number.")
            .Matches(@"[\!\?\*\.\@]+").WithMessage("Password must contain at least one special character (!? *.@).");

        RuleFor(x => x.TenantName)
            .NotEmpty().WithMessage("Tenant name is required")
            .MaximumLength(100).WithMessage("Tenant name must not exceed 100 characters");
    }
}
