namespace BudgetWise.Application.Reports.DTOs;

public sealed record CategoryAnalysisItem(
    Guid? CategoryId,
    string CategoryName,
    string? CategoryColor,
    string? CategoryIcon,
    decimal TotalAmount,
    int TransactionCount,
    decimal Percentage
);

public sealed record CategoryAnalysisReportResponse(
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalAmount,
    int TotalTransactions,
    IReadOnlyList<CategoryAnalysisItem> Items
);
