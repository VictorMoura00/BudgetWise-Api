using BudgetWise.Domain.Common.Pagination;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Valora.Domain.Common.Abstractions;

namespace BudgetWise.Domain.Common.Interfaces;

public interface IRepository<T> where T : IAggregateRoot
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaginatedList<T>> GetPaginatedAsync(int page, int pageSize, CancellationToken cancellationToken);
}