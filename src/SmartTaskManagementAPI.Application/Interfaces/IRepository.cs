using System;
using SmartTaskManagementAPI.Application.Common.Models;
using SmartTaskManagementAPI.Domain.Entities.Base;

namespace SmartTaskManagementAPI.Application.Interfaces;

public interface IRepository<T> where T : AuditableEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PaginatedResult<T>> GetPaginatedAsync(
        PaginationQuery pagination, 
        CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Delete(T entity);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
