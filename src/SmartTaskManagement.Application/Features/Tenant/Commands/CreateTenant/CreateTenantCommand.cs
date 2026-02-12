using FluentValidation;
using MediatR;
using SmartTaskManagement.Application.Common.Behaviors;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Application.Features.Tenants.Commands.CreateTenant;

[Authorize("Admin")] // Only admins can create tenants
[Transactional]
public class CreateTenantCommand : IRequest<Result<Guid>>
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, Result<Guid>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTenantCommandHandler(
        ITenantRepository tenantRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        CreateTenantCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrEmpty(userId))
            return Result<Guid>.Failure("User not authenticated");

        // Check if tenant name is unique
        var isNameUnique = await _tenantRepository.IsNameUniqueAsync(request.Name, null, cancellationToken);

        if (!isNameUnique)
            return Result<Guid>.Failure($"Tenant with name '{request.Name}' already exists");

        // Create tenant
        var tenant = new Tenant(
            request.Name,
            request.Description,
            userId);

        // Add to repository
        await _tenantRepository.AddAsync(tenant, cancellationToken);
        
        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(tenant.Id);
    }
}

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_]+$").WithMessage("Name can only contain letters, numbers, spaces, hyphens, and underscores");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");
    }
}