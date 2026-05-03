using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Interfaces;
using BudgetWise.Infrastructure.Persistence;
using BudgetWise.Infrastructure.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace BudgetWise.Infrastructure.Repositories;

public class SharedExpenseRepository(AppDbContext context)
    : Repository<SharedExpense>(context), ISharedExpenseRepository
{
    public async Task<IReadOnlyList<SharedExpense>> GetAllForGroupAsync(
        Guid familyGroupId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<SharedExpense>()
            .AsNoTracking()
            .Include(se => se.Participants)
            .Where(se => se.FamilyGroupId == familyGroupId)
            .OrderByDescending(se => se.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<SharedExpense?> GetByIdForGroupAsync(
        Guid id,
        Guid familyGroupId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<SharedExpense>()
            .Include(se => se.Participants)
            .FirstOrDefaultAsync(
                se => se.Id == id && se.FamilyGroupId == familyGroupId,
                cancellationToken);
    }
}
