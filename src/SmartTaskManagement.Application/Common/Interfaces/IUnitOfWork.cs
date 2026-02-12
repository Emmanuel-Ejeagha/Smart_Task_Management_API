namespace SmartTaskManagement.Application.Common.Interfaces;

/// <summary>
/// Unit of Work pattern for transaction management
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Save changes asynchronously
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begin a transaction
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commit the transaction
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback the transaction
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute in transaction
    /// </summary>
    Task ExecuteInTransactionAsync(
        Func<Task> action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish domain events
    /// </summary>
    Task DispatchDomainEventsAsync(CancellationToken cancellationToken = default);
}