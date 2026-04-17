namespace BudgetWise.Domain.Common.Pagination;

public sealed record MonthlySummaryResult(int Year, int Month, decimal Income, decimal Expense);
