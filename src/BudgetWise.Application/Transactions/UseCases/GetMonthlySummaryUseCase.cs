using BudgetWise.Application.Interfaces;
using BudgetWise.Application.Transactions.DTOs;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.Transactions.UseCases;

public sealed class GetMonthlySummaryUseCase(ITransactionRepository repository) : IUseCase
{
    public async Task<Result<IReadOnlyList<MonthlySummaryResponse>>> ExecuteAsync(
        Guid userId,
        int months,
        CancellationToken cancellationToken = default)
    {
        var clamped = months is < 1 or > 24 ? 6 : months;

        var items = await repository.GetMonthlySummaryForUserAsync(userId, clamped, cancellationToken);

        IReadOnlyList<MonthlySummaryResponse> response = items
            .Select(r => new MonthlySummaryResponse(
                Month: $"{r.Year:D4}-{r.Month:D2}",
                Income: r.Income,
                Expense: r.Expense))
            .ToList();

        return Result<IReadOnlyList<MonthlySummaryResponse>>.Success(response);
    }
}
