namespace BudgetWise.Domain.Common.Pagination;

public sealed record CategoryTotalResult(
    Guid? CategoryId,
    string CategoryName,
    string? CategoryColor,
    decimal Amount
);
