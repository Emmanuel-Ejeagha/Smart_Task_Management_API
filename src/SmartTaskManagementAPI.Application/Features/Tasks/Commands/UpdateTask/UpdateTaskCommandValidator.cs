using System;
using FluentValidation;

namespace SmartTaskManagementAPI.Application.Features.Tasks.Commands.UpdateTask;

public class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty().WithMessage("Task ID is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Task ID is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 character");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority value.");

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Due date must be in the future")
            .When(x => x.DueDate.HasValue);

        RuleFor(x => x.ReminderDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Reminder date must be in the future")
            .LessThan(x => x.DueDate).WithMessage("Reminder date must be before due date")
            .When(x => x.ReminderDate.HasValue && x.DueDate.HasValue);
    }
}
