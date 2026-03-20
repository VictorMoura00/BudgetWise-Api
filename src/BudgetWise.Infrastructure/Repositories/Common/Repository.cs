using BudgetWise.Domain.Common.Abstractions;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Common.Pagination;
using Microsoft.EntityFrameworkCore;

namespace BudgetWise.Infrastructure.Repositories.Common;

public abstract class Repository<T> : IRepository<T> where T : class, IAggregateRoot
{
    protected readonly DbContext Context;
    protected readonly DbSet<T> DbSet;

    protected Repository(DbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await DbSet.FindAsync([id], ct);

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
        => await DbSet.AsNoTracking().ToListAsync(ct);

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
        => await DbSet.AddAsync(entity, ct);

    public virtual Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        DbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(id, ct);
        if (entity != null) DbSet.Remove(entity);
    }

    public virtual async Task<PaginatedList<T>> GetPaginatedAsync(int page, int pageSize, CancellationToken ct)
    {
        var count = await DbSet.CountAsync(ct);
        var items = await DbSet.AsNoTracking()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedList<T>(items, count, page, pageSize);
    }
}