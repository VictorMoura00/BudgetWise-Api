using BudgetWise.Application.Interfaces;
using BudgetWise.Application.Transactions.DTOs;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.Transactions.UseCases;

public sealed class GetTransactionSummaryUseCase(ITransactionRepository repository) : IUseCase
{
    public async Task<Result<TransactionSummaryResponse>> ExecuteAsync(
        Guid userId,
        DateOnly? startDate,
        DateOnly? endDate,
        CancellationToken cancellationToken = default)
    {
        var result = await repository.GetSummaryForUserAsync(userId, startDate, endDate, cancellationToken);

        return new TransactionSummaryResponse(
            TotalIncome: result.TotalIncome,
            TotalExpense: result.TotalExpense,
            Balance: result.TotalIncome - result.TotalExpense,
            PendingCount: result.PendingCount,
            PendingAmount: result.PendingAmount);
    }
}
