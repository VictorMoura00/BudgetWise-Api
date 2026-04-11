using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Entities;

namespace BudgetWise.Domain.Interfaces;

public interface ITagRepository : IRepository<Tag>
{
    Task<IReadOnlyList<Tag>> GetAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Tag?> GetByIdForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(
        string name,
        Guid userId,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);
}
