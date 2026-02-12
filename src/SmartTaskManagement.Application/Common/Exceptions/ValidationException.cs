using SmartTaskManagement.Application.Common.Models;

namespace SmartTaskManagement.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : ApplicationException
{
    public List<ValidationError> Errors { get; }

    public ValidationException(List<ValidationError> errors)
        : base("Validation failed")
    {
        Errors = errors;
    }

    public ValidationException(string propertyName, string errorMessage)
        : base($"Validation failed for {propertyName}: {errorMessage}")
    {
        Errors = new List<ValidationError>
        {
            new ValidationError(propertyName, errorMessage)
        };
    }

    public override string ErrorCode => "VALIDATION_ERROR";
}