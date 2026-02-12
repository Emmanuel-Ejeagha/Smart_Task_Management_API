using System.Text.RegularExpressions;

namespace SmartTaskManagement.Domain.ValueObjects;

/// <summary>
/// Value object representing an email address
/// Implements validation and comparison logic
/// </summary>
public sealed class Email : IEquatable<Email>
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new Email value object with validation
    /// </summary>
    /// <param name="email">Email address string</param>
    /// <returns>Email value object</returns>
    /// <exception cref="ArgumentException">Thrown if email is invalid</exception>
    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or empty", nameof(email));

        email = email.Trim().ToLowerInvariant();

        if (email.Length > 254)
            throw new ArgumentException("Email is too long", nameof(email));

        if (!IsValidEmail(email))
            throw new ArgumentException("Invalid email format", nameof(email));

        return new Email(email);
    }

    /// <summary>
    /// Validates email format using regex
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        try
        {
            // Simple regex for basic validation
            // In production, consider using more comprehensive validation
            var regex = new Regex(
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | 
                System.Text.RegularExpressions.RegexOptions.Compiled);
            
            return regex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }

    public bool Equals(Email? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Email);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(Email? left, Email? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Email? left, Email? right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return Value;
    }
}