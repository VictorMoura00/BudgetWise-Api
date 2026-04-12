using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Common.Pagination;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;

namespace BudgetWise.Domain.Interfaces;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<PaginatedList<Transaction>> GetTransactionsForUserAsync(
        Guid userId,
        int page,
        int pageSize,
        TransactionType? type = null,
        Guid? categoryId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool? isConfirmed = null,
        CancellationToken cancellationToken = default);

    Task<Transaction?> GetByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> IsTagLinkedAsync(
        Guid transactionId,
        Guid tagId,
        CancellationToken cancellationToken = default);

    Task AddTagAsync(
        Guid transactionId,
        Guid tagId,
        CancellationToken cancellationToken = default);

    Task RemoveTagAsync(
        Guid transactionId,
        Guid tagId,
        CancellationToken cancellationToken = default);
}
