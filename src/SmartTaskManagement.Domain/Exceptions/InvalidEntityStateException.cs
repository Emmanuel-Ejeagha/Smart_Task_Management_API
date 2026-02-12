namespace SmartTaskManagement.Domain.Exceptions;

/// <summary>
/// Exception thrown when an entity is in an invalid state for an operation
/// </summary>
public class InvalidEntityStateException : DomainException
{
    public InvalidEntityStateException(string message)
        : base(message)
    {
    }

    public InvalidEntityStateException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}