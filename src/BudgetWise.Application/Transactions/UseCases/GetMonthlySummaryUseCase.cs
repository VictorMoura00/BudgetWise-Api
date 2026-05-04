using BudgetWise.Application.Interfaces;
using BudgetWise.Application.Transactions.DTOs;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.Transactions.UseCases;

public sealed class GetMonthlySummaryUseCase(ITransactionRepository repository) : IUseCase
{
    public async Task<Result<IReadOnlyList<MonthlySummaryResponse>>> ExecuteAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        TransactionType? type = null,
        bool? isConfirmed = null,
        Guid? categoryId = null,
        Guid? familyGroupId = null,
        PaymentMethod? paymentMethod = null,
        CancellationToken cancellationToken = default)
    {
        var items = await repository.GetMonthlySummaryForUserAsync(
            userId, startDate, endDate,
            type, isConfirmed, categoryId, familyGroupId, paymentMethod,
            cancellationToken);

        IReadOnlyList<MonthlySummaryResponse> response = items
            .Select(r => new MonthlySummaryResponse(
                Month: $"{r.Year:D4}-{r.Month:D2}",
                Income: r.Income,
                Expense: r.Expense))
            .ToList();

        return Result<IReadOnlyList<MonthlySummaryResponse>>.Success(response);
    }
}
