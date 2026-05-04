using BudgetWise.Domain.Enums;

namespace BudgetWise.Application.Dashboard.DTOs;

public sealed record PeriodInfo(
    DateOnly StartDate,
    DateOnly EndDate
);

public sealed record FinancialSummaryInfo(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Balance,
    decimal? SavingsRate,
    decimal ConfirmedBalance,
    decimal PendingImpact,
    decimal ProjectedBalance
);

public sealed record DueSummaryGroupInfo(int Count, decimal TotalAmount);

public sealed record PendingSummaryInfo(
    int TotalPendingCount,
    decimal TotalPendingAmount,
    DueSummaryGroupInfo Overdue,
    DueSummaryGroupInfo DueToday,
    DueSummaryGroupInfo DueNext7Days,
    DueSummaryGroupInfo DueNext30Days,
    DueSummaryGroupInfo Future,
    DueSummaryGroupInfo WithoutDueDate
);

public sealed record CategoryHighlightItem(
    Guid? CategoryId,
    string CategoryName,
    string? CategoryColor,
    decimal Amount,
    decimal? ChangePercent
);

public sealed record CategoryHighlightsInfo(
    int TotalExpenseCategories,
    CategoryHighlightItem? TopExpenseCategory,
    CategoryHighlightItem? TopIncomeCategory,
    CategoryHighlightItem? FastestGrowingCategory
);

public sealed record DashboardLargestExpense(
    Guid Id,
    string Description,
    decimal Amount,
    string? CategoryName,
    string? CategoryColor,
    DateOnly TransactionDate,
    DateOnly? DueDate
);

public sealed record MonthlyComparisonInfo(
    decimal PreviousIncome,
    decimal PreviousExpense,
    decimal IncomeDiff,
    decimal? IncomeDiffPercent,
    decimal ExpenseDiff,
    decimal? ExpenseDiffPercent
);

public sealed record DashboardOverviewResponse(
    PeriodInfo Period,
    FinancialSummaryInfo FinancialSummary,
    PendingSummaryInfo PendingSummary,
    CategoryHighlightsInfo CategoryHighlights,
    DashboardLargestExpense? LargestExpense,
    MonthlyComparisonInfo MonthlyComparison
);
