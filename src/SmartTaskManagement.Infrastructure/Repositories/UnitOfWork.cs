using Microsoft.EntityFrameworkCore.Storage;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Infrastructure.Data;
using SmartTaskManagement.Infrastructure.Services;

namespace SmartTaskManagement.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(
        ApplicationDbContext context,
        IDomainEventDispatcher domainEventDispatcher)
    {
        _context = context;
        _domainEventDispatcher = domainEventDispatcher;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
            return;

        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
            throw new InvalidOperationException("No transaction to commit");

        try
        {
            await SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            _currentTransaction?.Dispose();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
            throw new InvalidOperationException("No transaction to rollback");

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            _currentTransaction?.Dispose();
            _currentTransaction = null;
        }
    }

    public async Task ExecuteInTransactionAsync(
        Func<Task> action,
        CancellationToken cancellationToken = default)
    {
        await BeginTransactionAsync(cancellationToken);

        try
        {
            await action();
            await CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task DispatchDomainEventsAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = _context.GetDomainEvents();
        await _domainEventDispatcher.DispatchEventsAsync(domainEvents, cancellationToken);
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Get the current transaction if exists
    /// </summary>
    public IDbContextTransaction? GetCurrentTransaction()
    {
        return _currentTransaction;
    }

    /// <summary>
    /// Check if a transaction is active
    /// </summary>
    public bool HasActiveTransaction()
    {
        return _currentTransaction != null;
    }
}