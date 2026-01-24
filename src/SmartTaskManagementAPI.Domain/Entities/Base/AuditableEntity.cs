using System;

namespace SmartTaskManagementAPI.Domain.Entities.Base;

public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public DateTime? LastLoginAt { get; private set; }

    public void MarkAsCreated(Guid createdByUserId)
    {
        CreatedAt = DateTime.UtcNow;
        CreatedBy = createdByUserId;
    }

    public void MarkAsUpdated(Guid updatedByUserId)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedByUserId;
    }

    public void MarkAsDeleted(Guid deletedByUserId)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedByUserId;
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }

}
