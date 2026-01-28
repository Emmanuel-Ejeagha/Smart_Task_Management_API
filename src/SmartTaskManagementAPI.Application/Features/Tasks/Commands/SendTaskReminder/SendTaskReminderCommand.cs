using MediatR;

namespace SmartTaskManagementAPI.Application.Features.Tasks.Commands.SendTaskReminder;

public class SendTaskReminderCommand : IRequest<Unit>
{
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
}
