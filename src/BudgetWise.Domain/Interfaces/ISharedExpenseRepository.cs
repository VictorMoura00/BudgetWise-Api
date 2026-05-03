using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Entities;

namespace BudgetWise.Domain.Interfaces;

public interface ISharedExpenseRepository : IRepository<SharedExpense>
{
    Task<IReadOnlyList<SharedExpense>> GetAllForGroupAsync(
        Guid familyGroupId,
        CancellationToken cancellationToken = default);

    Task<SharedExpense?> GetByIdForGroupAsync(
        Guid id,
        Guid familyGroupId,
        CancellationToken cancellationToken = default);
}
