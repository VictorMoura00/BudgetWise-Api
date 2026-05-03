using BudgetWise.Application.Dashboard.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.Dashboard.UseCases;

public sealed class GetDashboardOverviewUseCase(
    ITransactionRepository repository,
    TimeProvider timeProvider) : IUseCase
{
    public async Task<Result<DashboardOverviewResponse>> ExecuteAsync(
        Guid userId,
        int? year = null,
        int? month = null,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().DateTime);
        var y = year ?? today.Year;
        var m = month ?? today.Month;

        var currentStart = new DateOnly(y, m, 1);
        var currentEnd = new DateOnly(y, m, DateTime.DaysInMonth(y, m));
        var previousEnd = currentStart.AddDays(-1);
        var previousStart = new DateOnly(previousEnd.Year, previousEnd.Month, 1);

        var currentSummary = await repository.GetSummaryForUserAsync(
            userId, currentStart, currentEnd, cancellationToken);

        var previousSummary = await repository.GetSummaryForUserAsync(
            userId, previousStart, previousEnd, cancellationToken);

        var projection = await repository.GetMonthProjectionForUserAsync(
            userId, y, m, cancellationToken);

        var largestExpense = await repository.GetLargestExpenseForUserAsync(
            userId, currentStart, currentEnd, cancellationToken);

        var categoryComparison = await repository.GetCategoryComparisonForUserAsync(
            userId, currentStart, currentEnd, previousStart, previousEnd, cancellationToken);

        var topIncomeCategory = await repository.GetTopCategoryByTypeAsync(
            userId, TransactionType.Income, currentStart, currentEnd, cancellationToken);

        var pendingTransactions = await repository.GetPendingForDueReportAsync(
            userId, cancellationToken);

        var savingsRate = currentSummary.TotalIncome > 0
            ? Math.Round((currentSummary.TotalIncome - currentSummary.TotalExpense)
                / currentSummary.TotalIncome * 100, 2)
            : (decimal?)null;

        var financialSummary = new FinancialSummaryInfo(
            currentSummary.TotalIncome,
            currentSummary.TotalExpense,
            currentSummary.TotalIncome - currentSummary.TotalExpense,
            savingsRate,
            projection.ConfirmedBalance,
            projection.PendingImpact,
            projection.ProjectedBalance);

        var pendingSummary = BuildPendingSummary(pendingTransactions, today);

        var topExpense = categoryComparison.FirstOrDefault();
        var topExpenseItem = topExpense is null ? null : new CategoryHighlightItem(
            topExpense.CategoryId,
            topExpense.CategoryName,
            topExpense.CategoryColor,
            topExpense.CurrentAmount,
            topExpense.PreviousAmount > 0
                ? Math.Round((topExpense.CurrentAmount - topExpense.PreviousAmount)
                    / topExpense.PreviousAmount * 100, 2)
                : null);

        var fastestGrowing = categoryComparison
            .Where(c => c.PreviousAmount > 0 && c.CurrentAmount > c.PreviousAmount)
            .OrderByDescending(c => (c.CurrentAmount - c.PreviousAmount) / c.PreviousAmount)
            .FirstOrDefault();
        var fastestGrowingItem = fastestGrowing is null ? null : new CategoryHighlightItem(
            fastestGrowing.CategoryId,
            fastestGrowing.CategoryName,
            fastestGrowing.CategoryColor,
            fastestGrowing.CurrentAmount,
            Math.Round((fastestGrowing.CurrentAmount - fastestGrowing.PreviousAmount)
                / fastestGrowing.PreviousAmount * 100, 2));

        var topIncomeItem = topIncomeCategory is null ? null : new CategoryHighlightItem(
            topIncomeCategory.CategoryId,
            topIncomeCategory.CategoryName,
            topIncomeCategory.CategoryColor,
            topIncomeCategory.Amount,
            null);

        var categoryHighlights = new CategoryHighlightsInfo(
            categoryComparison.Count(c => c.CurrentAmount > 0),
            topExpenseItem,
            topIncomeItem,
            fastestGrowingItem);

        DashboardLargestExpense? largestExpenseInfo = largestExpense is null ? null : new DashboardLargestExpense(
            largestExpense.Id,
            largestExpense.Description,
            largestExpense.Amount,
            largestExpense.Category?.Name,
            largestExpense.Category?.Color,
            largestExpense.TransactionDate,
            largestExpense.DueDate);

        var incomeDiff = currentSummary.TotalIncome - previousSummary.TotalIncome;
        var expenseDiff = currentSummary.TotalExpense - previousSummary.TotalExpense;
        var monthlyComparison = new MonthlyComparisonInfo(
            previousSummary.TotalIncome,
            previousSummary.TotalExpense,
            incomeDiff,
            previousSummary.TotalIncome > 0
                ? Math.Round(incomeDiff / previousSummary.TotalIncome * 100, 2)
                : null,
            expenseDiff,
            previousSummary.TotalExpense > 0
                ? Math.Round(expenseDiff / previousSummary.TotalExpense * 100, 2)
                : null);

        return Result<DashboardOverviewResponse>.Success(new DashboardOverviewResponse(
            Period: new PeriodInfo(y, m, currentStart, currentEnd),
            FinancialSummary: financialSummary,
            PendingSummary: pendingSummary,
            CategoryHighlights: categoryHighlights,
            LargestExpense: largestExpenseInfo,
            MonthlyComparison: monthlyComparison));
    }

    private static PendingSummaryInfo BuildPendingSummary(
        IReadOnlyList<Transaction> pending, DateOnly today)
    {
        var next7 = today.AddDays(7);
        var next30 = today.AddDays(30);

        var overdue = pending.Where(t => t.DueDate < today).ToList();
        var dueToday = pending.Where(t => t.DueDate == today).ToList();
        var dueNext7 = pending.Where(t => t.DueDate > today && t.DueDate <= next7).ToList();
        var dueNext30 = pending.Where(t => t.DueDate > next7 && t.DueDate <= next30).ToList();
        var future = pending.Where(t => t.DueDate > next30).ToList();
        var withoutDueDate = pending.Where(t => t.DueDate is null).ToList();

        return new PendingSummaryInfo(
            TotalPendingCount: pending.Count,
            TotalPendingAmount: pending.Sum(t => t.Amount),
            Overdue: new DueSummaryGroupInfo(overdue.Count, overdue.Sum(t => t.Amount)),
            DueToday: new DueSummaryGroupInfo(dueToday.Count, dueToday.Sum(t => t.Amount)),
            DueNext7Days: new DueSummaryGroupInfo(dueNext7.Count, dueNext7.Sum(t => t.Amount)),
            DueNext30Days: new DueSummaryGroupInfo(dueNext30.Count, dueNext30.Sum(t => t.Amount)),
            Future: new DueSummaryGroupInfo(future.Count, future.Sum(t => t.Amount)),
            WithoutDueDate: new DueSummaryGroupInfo(withoutDueDate.Count, withoutDueDate.Sum(t => t.Amount)));
    }
}
