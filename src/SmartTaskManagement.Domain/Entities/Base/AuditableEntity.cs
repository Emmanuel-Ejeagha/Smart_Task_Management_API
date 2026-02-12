using System.ComponentModel.DataAnnotations.Schema;

namespace SmartTaskManagement.Domain.Entities.Base;

/// <summary>
/// Base entity with audit fields for tracking creation and modification
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    
    /// <summary>
    /// When the entity was created (UTC)
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Who created the entity (from Auth0 claims)
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// When the entity was last updated (UTC)
    /// </summary>
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>
    /// Who last updated the entity (from Auth0 claims)
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// When the entity was deleted (UTC) - for soft delete
    /// </summary>
    public DateTime? DeletedAtUtc { get; set; }

    /// <summary>
    /// Who deleted the entity (from Auth0 claims) - for soft delete
    /// </summary>
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Whether the entity is deleted (soft delete)
    /// </summary>
    [NotMapped]
    public bool IsDeleted => DeletedAtUtc.HasValue;

    /// <summary>
    /// Method to update audit fields when entity is created
    /// </summary>
    /// <param name="createdBy">User ID from Auth0 claims</param>
    public void MarkAsCreated(string createdBy)
    {
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("CreatedBy cannot be null or empty", nameof(createdBy));

        CreatedBy = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Method to update audit fields when entity is updated
    /// </summary>
    /// <param name="updatedBy">User ID from Auth0 claims</param>
    public void MarkAsUpdated(string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("UpdatedBy cannot be null or empty", nameof(updatedBy));

        if (IsDeleted)
            throw new InvalidOperationException("Cannot update a deleted entity");

        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
        IncrementRowVersion();
    }

    /// <summary>
    /// Method to soft delete an entity
    /// </summary>
    /// <param name="deletedBy">User ID from Auth0 claims</param>
    public virtual void MarkAsDeleted(string deletedBy)
    {
        if (string.IsNullOrWhiteSpace(deletedBy))
            throw new ArgumentException("DeletedBy cannot be null or empty", nameof(deletedBy));

        DeletedBy = deletedBy;
        DeletedAtUtc = DateTime.UtcNow;
        IncrementRowVersion();
    }

    /// <summary>
    /// Method to restore a soft-deleted entity
    /// </summary>
    public virtual void Restore()
    {
        DeletedBy = null;
        DeletedAtUtc = null;
        IncrementRowVersion();
    }
}