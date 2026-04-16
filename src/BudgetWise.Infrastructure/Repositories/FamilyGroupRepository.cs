using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Interfaces;
using BudgetWise.Infrastructure.Persistence;
using BudgetWise.Infrastructure.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace BudgetWise.Infrastructure.Repositories;

public class FamilyGroupRepository(AppDbContext context) : Repository<FamilyGroup>(context), IFamilyGroupRepository
{
    public async Task<IReadOnlyList<FamilyGroup>> GetAllForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<FamilyGroup>()
            .AsNoTracking()
            .Include(fg => fg.Members)
            .Where(fg => fg.Members.Any(m => m.UserId == userId))
            .OrderBy(fg => fg.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<FamilyGroup?> GetByIdWithMembersAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<FamilyGroup>()
            .Include(fg => fg.Members)
            .FirstOrDefaultAsync(fg => fg.Id == id, cancellationToken);
    }

    public async Task<FamilyGroup?> GetByInviteCodeAsync(
        string inviteCode,
        CancellationToken cancellationToken = default)
    {
        var normalized = inviteCode.Trim().ToUpperInvariant();
        return await Context.Set<FamilyGroup>()
            .Include(fg => fg.Members)
            .FirstOrDefaultAsync(fg => fg.InviteCode == normalized, cancellationToken);
    }

    public async Task<int> CountGroupsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<FamilyGroup>()
            .CountAsync(fg => fg.Members.Any(m => m.UserId == userId), cancellationToken);
    }
}
