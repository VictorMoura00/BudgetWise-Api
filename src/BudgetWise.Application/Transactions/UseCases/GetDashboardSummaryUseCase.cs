using BudgetWise.Application.Interfaces;
using BudgetWise.Application.Transactions.DTOs;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.Transactions.UseCases;

public sealed class GetDashboardSummaryUseCase(ITransactionRepository repository, TimeProvider timeProvider) : IUseCase
{
    public async Task<Result<DashboardSummaryResponse>> ExecuteAsync(
        Guid userId,
        DateOnly? startDate,
        DateOnly? endDate,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().DateTime);
        var start = startDate ?? new DateOnly(today.Year, today.Month, 1);
        var end = endDate ?? new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

        var duration = end.DayNumber - start.DayNumber + 1;
        var previousEnd = start.AddDays(-1);
        var previousStart = previousEnd.AddDays(-(duration - 1));

        var summary = await repository.GetSummaryForUserAsync(userId, start, end, cancellationToken: cancellationToken);
        var largestExpense = await repository.GetLargestExpenseForUserAsync(userId, start, end, cancellationToken);
        var projection = await repository.GetMonthProjectionForUserAsync(userId, start, end, cancellationToken);
        var categoryComparison = await repository.GetCategoryComparisonForUserAsync(userId, start, end, previousStart, previousEnd, cancellationToken);

        var savingsRate = summary.TotalIncome > 0
            ? Math.Round((summary.TotalIncome - summary.TotalExpense) / summary.TotalIncome * 100, 2)
            : (decimal?)null;

        LargestExpenseInfo? largestExpenseInfo = largestExpense is null ? null : new LargestExpenseInfo(
            largestExpense.Id,
            largestExpense.Description,
            largestExpense.Amount,
            largestExpense.Category?.Name,
            largestExpense.Category?.Color,
            largestExpense.TransactionDate);

        IReadOnlyList<CategoryComparisonItem> categoryItems = categoryComparison
            .Select(c =>
            {
                var diff = c.CurrentAmount - c.PreviousAmount;
                var pct = c.PreviousAmount > 0
                    ? Math.Round(diff / c.PreviousAmount * 100, 2)
                    : (decimal?)null;
                return new CategoryComparisonItem(
                    c.CategoryId, c.CategoryName, c.CategoryColor,
                    c.CurrentAmount, c.PreviousAmount, diff, pct);
            })
            .ToList();

        return Result<DashboardSummaryResponse>.Success(new DashboardSummaryResponse(
            TotalIncome: summary.TotalIncome,
            TotalExpense: summary.TotalExpense,
            Balance: summary.TotalIncome - summary.TotalExpense,
            PendingCount: summary.PendingCount,
            PendingAmount: summary.PendingAmount,
            SavingsRate: savingsRate,
            LargestExpense: largestExpenseInfo,
            ConfirmedBalance: projection.ConfirmedBalance,
            PendingImpact: projection.PendingImpact,
            ProjectedBalance: projection.ProjectedBalance,
            CategoryComparison: categoryItems));
    }
}
