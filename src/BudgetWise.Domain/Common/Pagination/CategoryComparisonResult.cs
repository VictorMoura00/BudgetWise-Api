namespace BudgetWise.Domain.Common.Pagination;

public sealed record CategoryComparisonResult(
    Guid? CategoryId,
    string CategoryName,
    string? CategoryColor,
    decimal CurrentAmount,
    decimal PreviousAmount
);
