using SmartTaskManagement.Domain.Entities.Base;

namespace SmartTaskManagement.Domain.Entities;

/// <summary>
/// Represents a tenant in the system
/// Each tenant is isolated from other tenants
/// Users can only access their tenant's data
/// </summary>
public class Tenant : AuditableEntity
{
    // Private constructor for EF Core
    private Tenant() { }

    public Tenant(string name, string description, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        Name = name;
        Description = description;
        MarkAsCreated(createdBy);
    }

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    private readonly List<WorkItem> _workItems = new();
    public IReadOnlyCollection<WorkItem> WorkItems => _workItems.AsReadOnly();

    /// <summary>
    /// Update tenant information
    /// </summary>
    public void Update(string name, string description, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        Name = name;
        Description = description;
        MarkAsUpdated(updatedBy);
    }

    /// <summary>
    /// Activate the tenant
    /// </summary>
    public void Activate(string updatedBy)
    {
        if (IsActive) return;

        IsActive = true;
        MarkAsUpdated(updatedBy);
    }

    /// <summary>
    /// Deactivate the tenant
    /// </summary>
    public void Deactivate(string updatedBy)
    {
        if (!IsActive) return;

        IsActive = false;
        MarkAsUpdated(updatedBy);
    }

    /// <summary>
    /// Override soft delete to also deactivate
    /// </summary>
    public override void MarkAsDeleted(string deletedBy)
    {
        base.MarkAsDeleted(deletedBy);
        IsActive = false;
    }

    /// <summary>
    /// Override restore to reactivate
    /// </summary>
    public override void Restore()
    {
        base.Restore();
        IsActive = true;
    }
}