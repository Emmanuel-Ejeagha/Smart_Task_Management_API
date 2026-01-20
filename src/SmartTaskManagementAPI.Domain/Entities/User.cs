using System;
using SmartTaskManagementAPI.Domain.Entities.Base;
using SmartTaskManagementAPI.Domain.ValueObjects;

namespace SmartTaskManagementAPI.Domain.Entities;

public class User : AuditableEntity
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool EmailConfirmed { get; private set; }
    public string Role { get; private set; } = "User";

    // Forign keys
    public Guid TenantId { get; private set; }

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual ICollection<Task> CreatedTasks { get; set; } = new List<Task>();
    public virtual ICollection<Task> UpdatedTasks { get; set; } = new List<Task>();

    private User() { }

    public static User Create(
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        Guid tenantId,
        Guid createdBy,
        string role = "User")
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty", nameof(lastName));

        if (!ValueObjects.Email.IsValid(email))
            throw new ArgumentException("Invalid email", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password cannot be empty", nameof(passwordHash));

        var user = new User
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.ToLowerInvariant().Trim(),
            NormalizedEmail = email.ToUpperInvariant().Trim(),
            PasswordHash = passwordHash,
            TenantId = tenantId,
            Role = role,
            IsActive = true,
            EmailConfirmed = false
        };

        user.MarkAsCreated(createdBy);
        return user;
    }

    public void Update(
        string firstName,
        string lastName,
        string? phoneNumber,
        Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(nameof(firstName)))
            throw new ArgumentException("First name cannot be empty", nameof(firstName));

        if (string.IsNullOrWhiteSpace(nameof(lastName)))
            throw new ArgumentException("Last name cannot be empty", nameof(lastName));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PhoneNumber = phoneNumber?.Trim();

        MarkAsUpdated(updatedBy);
    }

    public void ConfirmEmail()
    {
        EmailConfirmed = true;
    }

    public void ChangePassword(string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
            throw new ArgumentException("password hash cannot be empty", nameof(newPassword));

        PasswordHash = newPassword;
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

    public void ChangeRole(string newRole, Guid changedBy)
    {
        if (string.IsNullOrWhiteSpace(newRole))
            throw new ArgumentException("Role cannot be empty", nameof(newRole));

        Role = newRole;
        MarkAsUpdated(changedBy);
    }

    public string GetFullName() => $"{FirstName} {LastName}".Trim();
}
