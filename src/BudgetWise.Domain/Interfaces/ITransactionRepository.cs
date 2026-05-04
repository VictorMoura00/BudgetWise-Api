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
        Guid? familyGroupId = null,
        PaymentMethod? paymentMethod = null,
        CancellationToken cancellationToken = default);

    Task<Transaction?> GetByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<TransactionSummaryResult> GetSummaryForUserAsync(
        Guid userId,
        DateOnly? startDate,
        DateOnly? endDate,
        TransactionType? type = null,
        bool? isConfirmed = null,
        Guid? categoryId = null,
        Guid? familyGroupId = null,
        PaymentMethod? paymentMethod = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MonthlySummaryResult>> GetMonthlySummaryForUserAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        TransactionType? type = null,
        bool? isConfirmed = null,
        Guid? categoryId = null,
        Guid? familyGroupId = null,
        PaymentMethod? paymentMethod = null,
        CancellationToken cancellationToken = default);

    Task<Transaction?> GetLargestExpenseForUserAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    Task<MonthProjectionResult> GetMonthProjectionForUserAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryComparisonResult>> GetCategoryComparisonForUserAsync(
        Guid userId,
        DateOnly currentStart,
        DateOnly currentEnd,
        DateOnly previousStart,
        DateOnly previousEnd,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> GetPendingForDueReportAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<CategoryTotalResult?> GetTopCategoryByTypeAsync(
        Guid userId,
        TransactionType type,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryAnalysisResult>> GetCategoryAnalysisForUserAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        TransactionType? type = null,
        bool? isConfirmed = null,
        Guid? categoryId = null,
        Guid? familyGroupId = null,
        PaymentMethod? paymentMethod = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> GetTransactionsForPaymentStatusAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        TransactionType? type = null,
        Guid? categoryId = null,
        Guid? familyGroupId = null,
        PaymentMethod? paymentMethod = null,
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
