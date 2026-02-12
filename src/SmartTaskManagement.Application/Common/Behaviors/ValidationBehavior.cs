using FluentValidation;
using MediatR;
using SmartTaskManagement.Application.Common.Models;

namespace SmartTaskManagement.Application.Common.Behaviors;

/// <summary>
/// Validation behavior for MediatR pipeline
/// Automatically validates requests using FluentValidation
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            // Create validation errors list
            var validationErrors = failures
                .Select(f => new ValidationError(
                    f.PropertyName,
                    f.ErrorMessage,
                    f.ErrorCode))
                .ToList();

            // Check if response is Result<T>
            var responseType = typeof(TResponse);

            if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var resultType = responseType.GetGenericArguments()[0];
                var failureMethod = typeof(Result<>)
                    .MakeGenericType(resultType)
                    .GetMethod("ValidationFailure", new[] { typeof(List<ValidationError>) });

                if (failureMethod != null)
                {
                    return (TResponse)failureMethod.Invoke(null, new object[] { validationErrors })!;
                }
            }
            else if (responseType == typeof(Result))
            {
                var result = Result.ValidationFailure(validationErrors);
                return (TResponse)(object)result;
            }

            throw new ValidationException(failures);
        }

        return await next();
    }
}