namespace BudgetWise.Domain.Common.Pagination;

public sealed record TransactionSummaryResult(
    decimal TotalIncome,
    decimal TotalExpense,
    int PendingCount,
    decimal PendingAmount
);
