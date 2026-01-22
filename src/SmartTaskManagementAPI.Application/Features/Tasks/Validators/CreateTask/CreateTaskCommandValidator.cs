using FluentValidation;
using SmartTaskManagementAPI.Application.Features.Tasks.Commands.CreateTask;


namespace SmartTaskManagementAPI.Application.Features.Tasks.Validators.CreateTask;

public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(t => t.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(d => d.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        RuleFor(p => p.Priority)
            .IsInEnum().WithMessage("Invalid priority value.");

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Due date must be in the future.")
            .When(x => x.DueDate.HasValue);

        RuleFor(x => x.ReminderDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Reminder date must be in the future.")
            .LessThan(x => x.DueDate).WithMessage("Reminder date must be before due date")
            .When(x => x.ReminderDate.HasValue && x.DueDate.HasValue);
    }
}
