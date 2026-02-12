namespace SmartTaskManagement.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when user is forbidden from performing an action
/// </summary>
public class ForbiddenException : ApplicationException
{
    public ForbiddenException(string message)
        : base(message)
    {
    }

    public ForbiddenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public override string ErrorCode => "FORBIDDEN";
}