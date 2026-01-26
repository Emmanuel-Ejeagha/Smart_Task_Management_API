using System;
using FluentValidation;

namespace SmartTaskManagementAPI.Application.Features.Tasks.Commands.ArchiveTask;

public class ArchiveTaskCommandValidator : AbstractValidator<ArchiveTaskCommand>
{
    public ArchiveTaskCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty().WithMessage("Task ID is required");
    }
}
