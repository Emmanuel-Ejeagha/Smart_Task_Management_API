using SmartTaskManagementAPI.Domain.Entities.Base;

namespace SmartTaskManagementAPI.Application.Common.Interfaces;

public interface IDomainEventService
{
    Task PublishAsync(BaseEvent domainEvent);
}
