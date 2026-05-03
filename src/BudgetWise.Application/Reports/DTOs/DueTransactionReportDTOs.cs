using BudgetWise.Domain.Enums;

namespace BudgetWise.Application.Reports.DTOs;

public sealed record DueTransactionItem(
    Guid Id,
    string Description,
    decimal Amount,
    TransactionType Type,
    DateOnly TransactionDate,
    DateOnly? DueDate,
    Guid? CategoryId,
    string? CategoryName,
    string? CategoryColor
);

public sealed record DueTransactionGroup(
    int Count,
    decimal TotalAmount,
    IReadOnlyList<DueTransactionItem> Items
);

public sealed record DueTransactionsReportResponse(
    DateOnly ReportDate,
    int TotalPendingCount,
    decimal TotalPendingAmount,
    DueTransactionGroup Overdue,
    DueTransactionGroup DueToday,
    DueTransactionGroup DueNext7Days,
    DueTransactionGroup DueNext30Days,
    DueTransactionGroup Future,
    DueTransactionGroup WithoutDueDate
);
