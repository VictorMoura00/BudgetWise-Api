namespace BudgetWise.Domain.Common.Pagination;

public sealed record CategoryAnalysisResult(
    Guid? CategoryId,
    string CategoryName,
    string? CategoryColor,
    string? CategoryIcon,
    decimal TotalAmount,
    int TransactionCount
);
