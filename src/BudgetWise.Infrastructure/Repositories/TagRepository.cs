using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Interfaces;
using BudgetWise.Infrastructure.Persistence;
using BudgetWise.Infrastructure.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace BudgetWise.Infrastructure.Repositories;

public class TagRepository(AppDbContext context) : Repository<Tag>(context), ITagRepository
{
    public async Task<IReadOnlyList<Tag>> GetAllForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Tag>()
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tag?> GetByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Tag>()
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        string name,
        Guid userId,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLowerInvariant();
        return await Context.Set<Tag>()
            .AnyAsync(t =>
                t.Name == normalized &&
                t.UserId == userId &&
                (excludeId == null || t.Id != excludeId),
                cancellationToken);
    }
}
