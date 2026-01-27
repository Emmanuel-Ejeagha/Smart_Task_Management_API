using System;
using FluentValidation;

namespace SmartTaskManagementAPI.Application.Features.Tasks.Queries.GetTasks;

public class GetTasksQueryValidator : AbstractValidator<GetTasksQuery>
{
    public GetTasksQueryValidator()
    {
        RuleFor(x => x.Pagination.PageNumber)
            .GreaterThan(0).WithMessage("page number must be greater than 0.");

        RuleFor(x => x.Pagination.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0.")
            .LessThanOrEqualTo(100).WithMessage("Page size must not exceed 100")
            .When(x => x.Pagination != null);

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid task status value")
            .When(x => x.Status.HasValue);

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid task priority value.")
            .When(x => x.Priority.HasValue);
    }
}
