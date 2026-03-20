using BudgetWise.Domain.Common.Pagination;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Interfaces;
using BudgetWise.Infrastructure.Persistence;
using BudgetWise.Infrastructure.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace BudgetWise.Infrastructure.Repositories;

public class CategoryRepository(AppDbContext context) : Repository<Category>(context), ICategoryRepository
{
    public async Task<PaginatedList<Category>> GetCategoriesForUserAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Category>()
            .AsNoTracking()
            .Where(c => (c.UserId == null && c.IsSystem) ||
                        (c.UserId == userId && c.IsActive))
            .OrderBy(c => !c.IsSystem)
            .ThenBy(c => c.Name);

        var count = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<Category>(items, count, page, pageSize);
    }

    public async Task<Category?> GetByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Category>()
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                ((c.IsSystem && c.UserId == null) || c.UserId == userId),
                cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        string name,
        Guid userId,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Category>()
            .AnyAsync(c =>
                c.Name == name &&
                c.UserId == userId &&
                (excludeId == null || c.Id != excludeId),
                cancellationToken);
    }
}