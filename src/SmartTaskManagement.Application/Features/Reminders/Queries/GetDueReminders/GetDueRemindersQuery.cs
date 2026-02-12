using AutoMapper;
using FluentValidation;
using MediatR;
using SmartTaskManagement.Application.Common.Behaviors;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.Reminders.Dtos;

namespace SmartTaskManagement.Application.Features.Reminders.Queries.GetDueReminders;

[Authorize("Admin")] // Typically only admins or background jobs check due reminders
public class GetDueRemindersQuery : IRequest<Result<IReadOnlyList<ReminderDto>>>
{
    public DateTime? AsOfDateUtc { get; set; }
    public int? Limit { get; set; }
}

public class GetDueRemindersQueryHandler : IRequestHandler<GetDueRemindersQuery, Result<IReadOnlyList<ReminderDto>>>
{
    private readonly IReminderRepository _reminderRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetDueRemindersQueryHandler(
        IReminderRepository reminderRepository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _reminderRepository = reminderRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<ReminderDto>>> Handle(
        GetDueRemindersQuery request,
        CancellationToken cancellationToken)
    {
        // Check if user is admin
        if (!_currentUserService.IsAdmin)
            return Result<IReadOnlyList<ReminderDto>>.Failure("Only admins can view due reminders");

        // Get due reminders
        var asOfDate = request.AsOfDateUtc ?? DateTime.UtcNow;
        var dueReminders = await _reminderRepository.GetDueRemindersAsync(asOfDate, cancellationToken);

        // Apply limit if specified
        if (request.Limit.HasValue && request.Limit.Value > 0)
        {
            dueReminders = dueReminders.Take(request.Limit.Value).ToList();
        }

        // Map to DTOs
        var dtos = _mapper.Map<IReadOnlyList<ReminderDto>>(dueReminders);

        // Set additional properties
        foreach (var dto in dtos)
        {
            dto.IsPending = false; // They're due, so not pending anymore
            dto.IsDue = true;
        }

        return Result<IReadOnlyList<ReminderDto>>.Success(dtos);
    }
}

public class GetDueRemindersQueryValidator : AbstractValidator<GetDueRemindersQuery>
{
    public GetDueRemindersQueryValidator()
    {
        RuleFor(x => x.AsOfDateUtc)
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("As of date cannot be more than 5 minutes in the future")
            .When(x => x.AsOfDateUtc.HasValue);

        RuleFor(x => x.Limit)
            .GreaterThan(0).WithMessage("Limit must be greater than 0")
            .LessThanOrEqualTo(1000).WithMessage("Limit cannot exceed 1000")
            .When(x => x.Limit.HasValue);
    }
}