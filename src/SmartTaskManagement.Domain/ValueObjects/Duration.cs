namespace SmartTaskManagement.Domain.ValueObjects;

/// <summary>
/// Value object representing a time duration
/// </summary>
public sealed class Duration : IEquatable<Duration>
{
    public TimeSpan Value { get; }

    private Duration(TimeSpan value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new Duration from hours
    /// </summary>
    public static Duration FromHours(double hours)
    {
        if (hours < 0)
            throw new ArgumentException("Duration cannot be negative", nameof(hours));

        return new Duration(TimeSpan.FromHours(hours));
    }

    /// <summary>
    /// Creates a new Duration from minutes
    /// </summary>
    public static Duration FromMinutes(double minutes)
    {
        if (minutes < 0)
            throw new ArgumentException("Duration cannot be negative", nameof(minutes));

        return new Duration(TimeSpan.FromMinutes(minutes));
    }

    /// <summary>
    /// Creates a new Duration from days
    /// </summary>
    public static Duration FromDays(double days)
    {
        if (days < 0)
            throw new ArgumentException("Duration cannot be negative", nameof(days));

        return new Duration(TimeSpan.FromDays(days));
    }

    /// <summary>
    /// Creates a new Duration from TimeSpan
    /// </summary>
    public static Duration FromTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan < TimeSpan.Zero)
            throw new ArgumentException("Duration cannot be negative", nameof(timeSpan));

        return new Duration(timeSpan);
    }

    /// <summary>
    /// Adds two durations
    /// </summary>
    public Duration Add(Duration other)
    {
        return new Duration(Value + other.Value);
    }

    /// <summary>
    /// Subtracts two durations
    /// </summary>
    public Duration Subtract(Duration other)
    {
        var result = Value - other.Value;
        if (result < TimeSpan.Zero)
            throw new InvalidOperationException("Resulting duration cannot be negative");

        return new Duration(result);
    }

    public bool Equals(Duration? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Duration);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(Duration? left, Duration? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Duration? left, Duration? right)
    {
        return !(left == right);
    }

    public static Duration operator +(Duration left, Duration right)
    {
        return left.Add(right);
    }

    public static Duration operator -(Duration left, Duration right)
    {
        return left.Subtract(right);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}