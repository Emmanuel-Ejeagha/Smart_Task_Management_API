namespace SmartTaskManagement.Domain.Entities.Base;

/// <summary>
/// Base entity with common properties for all entities
/// Uses Guid for ID to avoid sequential exposure
/// </summary>
public abstract class BaseEntity 
{
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
    }

    protected BaseEntity(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; private set; }
    
    /// <summary>
    /// Optimistic concurrency control using row version
    /// </summary>
    public uint RowVersion { get; private set; }

    /// <summary>
    /// Method to increment row version for concurrency control
    /// </summary>
    public void IncrementRowVersion()
    {
        RowVersion++;
    }

    /// <summary>
    /// Domain events that will be raised after entity is persisted
    /// </summary>
    private readonly List<object> _domainEvents = new();

    /// <summary>
    /// Read-only collection of domain events
    /// </summary>
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Add a domain event to the entity
    /// </summary>
    /// <param name="domainEvent">Domain event to add</param>
    public void AddDomainEvent(object domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Remove a domain event from the entity
    /// </summary>
    /// <param name="domainEvent">Domain event to remove</param>
    public void RemoveDomainEvent(object domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    /// <summary>
    /// Clear all domain events
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Equality check based on Id
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        if (Id == Guid.Empty || other.Id == Guid.Empty)
            return false;

        return Id == other.Id;
    }

    public static bool operator ==(BaseEntity? a, BaseEntity? b)
    {
        if (a is null && b is null)
            return true;

        if (a is null || b is null)
            return false;

        return a.Equals(b);
    }

    public static bool operator !=(BaseEntity? a, BaseEntity? b)
    {
        return !(a == b);
    }

    public override int GetHashCode()
    {
        return (GetType().ToString() + Id).GetHashCode();
    }
}