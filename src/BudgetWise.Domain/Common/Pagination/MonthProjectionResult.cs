namespace BudgetWise.Domain.Common.Pagination;

public sealed record MonthProjectionResult(
    decimal ConfirmedBalance,
    decimal PendingImpact,
    decimal ProjectedBalance
);
