using BudgetWise.Domain.Enums;

namespace BudgetWise.Application.Reports.DTOs;

public sealed record PaymentStatusGroup(
    int Count,
    decimal TotalAmount
);

public sealed record PaymentStatusReportResponse(
    DateOnly ReportDate,
    DateOnly StartDate,
    DateOnly EndDate,
    PaymentStatusGroup Confirmed,
    PaymentStatusGroup Pending,
    PaymentStatusGroup Overdue,
    PaymentStatusGroup DueToday,
    PaymentStatusGroup DueNext7Days,
    PaymentStatusGroup WithoutDueDate
);
