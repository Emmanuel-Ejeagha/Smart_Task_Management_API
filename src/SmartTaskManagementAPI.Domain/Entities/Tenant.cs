using System;
using SmartTaskManagementAPI.Domain.Entities.Base;

namespace SmartTaskManagementAPI.Domain.Entities;

public class Tenant : AuditableEntity

{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    // Navigation Properties
    public virtual ICollection<User> Users { get; private set; } = new List<User>();
    public virtual ICollection<User> Tasks { get; private set; } = new List<User>();

    private Tenant() { } // For Ef Core

    public static Tenant Create(string name, string slug, Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Tenant slug cannot be empty", nameof(slug));

        var tenant = new Tenant
        {
            Name = name.Trim(),
            Slug = slug.ToLowerInvariant().Trim(),
            IsActive = true
        };

        tenant.MarkAsCreated(createdBy);
        return tenant;
    }

    public void Update(string name, Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name cannot be empty");

        Name = name.Trim();
        MarkAsUpdated(updatedBy);
    }

    public void Deactivate(Guid deactivatedBy)
    {
        IsActive = false;
        MarkAsUpdated(deactivatedBy);
    }
    
    public void Activate(Guid activatedBy)
    {
        IsActive = true;
        MarkAsUpdated(activatedBy);
    }
}
