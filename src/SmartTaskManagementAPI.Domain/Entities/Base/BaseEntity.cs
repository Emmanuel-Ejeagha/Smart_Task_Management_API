using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartTaskManagementAPI.Domain.Entities.Base;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [NotMapped]
    public List<BaseEvent> DomainEvents { get; } = new();

    public void AddDomainEvent(BaseEvent domainEvent)
    {
        DomainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(BaseEvent domainEvent)
    {
        DomainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        DomainEvents.Clear();
    }
}

public abstract class BaseEvent
{
    public DateTime DateOccurred { get; protected set; } = DateTime.UtcNow;
}
