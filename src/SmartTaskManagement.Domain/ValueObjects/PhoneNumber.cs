namespace SmartTaskManagement.Domain.ValueObjects;

/// <summary>
/// Value object representing a phone number
/// Implements validation and comparison logic
/// </summary>
public sealed class PhoneNumber : IEquatable<PhoneNumber>
{
    public string Value { get; }

    private PhoneNumber(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new PhoneNumber value object with validation
    /// </summary>
    /// <param name="phoneNumber">Phone number string</param>
    /// <returns>PhoneNumber value object</returns>
    /// <exception cref="ArgumentException">Thrown if phone number is invalid</exception>
    public static PhoneNumber Create(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number cannot be null or empty", nameof(phoneNumber));

        // Remove all non-digit characters
        var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

        // Basic validation - adjust for your needs
        if (digitsOnly.Length < 10 || digitsOnly.Length > 15)
            throw new ArgumentException("Invalid phone number length", nameof(phoneNumber));

        // Format as E.164 if possible
        var formatted = $"+{digitsOnly}";
        
        return new PhoneNumber(formatted);
    }

    public bool Equals(PhoneNumber? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as PhoneNumber);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(PhoneNumber? left, PhoneNumber? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(PhoneNumber? left, PhoneNumber? right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return Value;
    }
}