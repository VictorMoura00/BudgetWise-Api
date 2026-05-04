using BudgetWise.Application.Interfaces;
using BudgetWise.Application.Reports.DTOs;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.Reports.UseCases;

public sealed class GetPaymentStatusReportUseCase(
    ITransactionRepository repository,
    TimeProvider timeProvider) : IUseCase
{
    public async Task<Result<PaymentStatusReportResponse>> ExecuteAsync(
        Guid userId,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        TransactionType? type = null,
        Guid? categoryId = null,
        Guid? familyGroupId = null,
        PaymentMethod? paymentMethod = null,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().DateTime);
        var start = startDate ?? new DateOnly(today.Year, today.Month, 1);
        var end = endDate ?? new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

        if (start > end)
            return Result<PaymentStatusReportResponse>.Failure(
                Error.Validation("PaymentStatus.InvalidDateRange", "startDate must be less than or equal to endDate."));

        var transactions = await repository.GetTransactionsForPaymentStatusAsync(
            userId, start, end, type, categoryId, familyGroupId, paymentMethod, cancellationToken);

        var next7 = today.AddDays(7);

        var confirmed = transactions.Where(t => t.IsConfirmed).ToList();
        var pending = transactions.Where(t => !t.IsConfirmed).ToList();
        var overdue = pending.Where(t => t.DueDate.HasValue && t.DueDate < today).ToList();
        var dueToday = pending.Where(t => t.DueDate == today).ToList();
        var dueNext7 = pending.Where(t => t.DueDate > today && t.DueDate <= next7).ToList();
        var withoutDueDate = pending.Where(t => t.DueDate is null).ToList();

        return Result<PaymentStatusReportResponse>.Success(new PaymentStatusReportResponse(
            ReportDate: today,
            StartDate: start,
            EndDate: end,
            Confirmed: Group(confirmed),
            Pending: Group(pending),
            Overdue: Group(overdue),
            DueToday: Group(dueToday),
            DueNext7Days: Group(dueNext7),
            WithoutDueDate: Group(withoutDueDate)));
    }

    private static PaymentStatusGroup Group<T>(IReadOnlyList<T> items) where T : Domain.Entities.Transaction =>
        new(items.Count, items.Sum(t => t.Amount));
}
