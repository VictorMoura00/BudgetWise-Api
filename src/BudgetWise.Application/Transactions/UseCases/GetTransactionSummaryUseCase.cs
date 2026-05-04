using BudgetWise.Application.Interfaces;
using BudgetWise.Application.Transactions.DTOs;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.Transactions.UseCases;

public sealed class GetTransactionSummaryUseCase(ITransactionRepository repository) : IUseCase
{
    public async Task<Result<TransactionSummaryResponse>> ExecuteAsync(
        Guid userId,
        DateOnly? startDate,
        DateOnly? endDate,
        TransactionType? type = null,
        bool? isConfirmed = null,
        Guid? categoryId = null,
        Guid? familyGroupId = null,
        PaymentMethod? paymentMethod = null,
        CancellationToken cancellationToken = default)
    {
        var result = await repository.GetSummaryForUserAsync(
            userId, startDate, endDate,
            type, isConfirmed, categoryId, familyGroupId, paymentMethod,
            cancellationToken);

        return new TransactionSummaryResponse(
            TotalIncome: result.TotalIncome,
            TotalExpense: result.TotalExpense,
            Balance: result.TotalIncome - result.TotalExpense,
            PendingCount: result.PendingCount,
            PendingAmount: result.PendingAmount);
    }
}
