using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Entities;

namespace BudgetWise.Domain.Interfaces;

public interface IFamilyGroupRepository : IRepository<FamilyGroup>
{
    Task<IReadOnlyList<FamilyGroup>> GetAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<FamilyGroup?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FamilyGroup?> GetByInviteCodeAsync(string inviteCode, CancellationToken cancellationToken = default);

    Task<int> CountGroupsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
