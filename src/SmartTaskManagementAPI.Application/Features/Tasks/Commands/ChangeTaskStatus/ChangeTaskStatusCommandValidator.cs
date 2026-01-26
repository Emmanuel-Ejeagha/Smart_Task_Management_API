using System;
using FluentValidation;
using SmartTaskManagementAPI.Domain.Enums;

namespace SmartTaskManagementAPI.Application.Features.Tasks.Commands.ChangeTaskStatus;

public class ChangeTaskStatusCommandValidator : AbstractValidator<ChangeTaskStatusCommand>
{
    public ChangeTaskStatusCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty().WithMessage("Task ID is required");

        RuleFor(x => x.NewStatus)
            .IsInEnum().WithMessage("Invalid task status value.")
            .Must(status => status != TasksStatus.Archived)
            .WithMessage("Use ArchiveTask command to archive a task");
    }
}
