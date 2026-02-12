using MediatR;
using Microsoft.Extensions.Logging;
using SmartTaskManagement.Application.Common.Interfaces;

namespace SmartTaskManagement.Application.Common.Behaviors;

/// <summary>
/// Transaction behavior for MediatR pipeline
/// Wraps request execution in a transaction
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        IUnitOfWork unitOfWork,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var hasTransactionAttribute = request.GetType()
            .GetCustomAttributes(typeof(TransactionalAttribute), true)
            .Any();

        // Only wrap in transaction if explicitly requested
        if (!hasTransactionAttribute)
            return await next();

        _logger.LogInformation(
            "Beginning transaction for {RequestName}",
            requestName);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();
            
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            
            _logger.LogInformation(
                "Committed transaction for {RequestName}",
                requestName);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Rolling back transaction for {RequestName} due to error",
                requestName);
            
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            // Ensure any resources are cleaned up
            // In a real implementation, you might need to ensure the transaction is disposed
        }
    }
}

/// <summary>
/// Attribute to mark requests that should be executed in a transaction
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class TransactionalAttribute : Attribute
{
}