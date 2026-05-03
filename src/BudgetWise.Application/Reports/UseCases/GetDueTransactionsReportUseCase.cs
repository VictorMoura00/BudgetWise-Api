using BudgetWise.Application.Interfaces;
using BudgetWise.Application.Reports.DTOs;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.Reports.UseCases;

public sealed class GetDueTransactionsReportUseCase(
    ITransactionRepository repository,
    TimeProvider timeProvider) : IUseCase
{
    private const int ItemsPerGroupLimit = 10;

    public async Task<Result<DueTransactionsReportResponse>> ExecuteAsync(
        Guid userId,
        bool includeItems = false,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().DateTime);
        var next7 = today.AddDays(7);
        var next30 = today.AddDays(30);

        var transactions = await repository.GetPendingForDueReportAsync(userId, cancellationToken);

        var overdue = transactions.Where(t => t.DueDate < today).ToList();
        var dueToday = transactions.Where(t => t.DueDate == today).ToList();
        var dueNext7 = transactions.Where(t => t.DueDate > today && t.DueDate <= next7).ToList();
        var dueNext30 = transactions.Where(t => t.DueDate > next7 && t.DueDate <= next30).ToList();
        var future = transactions.Where(t => t.DueDate > next30).ToList();
        var withoutDueDate = transactions.Where(t => t.DueDate is null).ToList();

        return Result<DueTransactionsReportResponse>.Success(new DueTransactionsReportResponse(
            ReportDate: today,
            TotalPendingCount: transactions.Count,
            TotalPendingAmount: transactions.Sum(t => t.Amount),
            Overdue: ToGroup(overdue, includeItems),
            DueToday: ToGroup(dueToday, includeItems),
            DueNext7Days: ToGroup(dueNext7, includeItems),
            DueNext30Days: ToGroup(dueNext30, includeItems),
            Future: ToGroup(future, includeItems),
            WithoutDueDate: ToGroup(withoutDueDate, includeItems)));
    }

    private static DueTransactionGroup ToGroup(List<Transaction> items, bool includeItems)
    {
        var totalAmount = items.Sum(t => t.Amount);
        var capped = includeItems
            ? items.Take(ItemsPerGroupLimit).Select(ToItem).ToList().AsReadOnly()
            : (IReadOnlyList<DueTransactionItem>)[];

        return new DueTransactionGroup(items.Count, totalAmount, capped);
    }

    private static DueTransactionItem ToItem(Transaction t) => new(
        t.Id,
        t.Description,
        t.Amount,
        t.Type,
        t.TransactionDate,
        t.DueDate,
        t.CategoryId,
        t.Category?.Name,
        t.Category?.Color);
}
