using BudgetWise.Domain.Common.Pagination;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Interfaces;
using BudgetWise.Infrastructure.Persistence;
using BudgetWise.Infrastructure.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace BudgetWise.Infrastructure.Repositories;

public class TransactionRepository(AppDbContext context) : Repository<Transaction>(context), ITransactionRepository
{
    public async Task<PaginatedList<Transaction>> GetTransactionsForUserAsync(
        Guid userId,
        int page,
        int pageSize,
        TransactionType? type = null,
        Guid? categoryId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool? isConfirmed = null,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Transaction>()
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.TransactionTags)
                .ThenInclude(tt => tt.Tag)
            .Where(t => t.UserId == userId && t.DeletedAt == null);

        if (type is not null)
            query = query.Where(t => t.Type == type);

        if (categoryId is not null)
            query = query.Where(t => t.CategoryId == categoryId);

        if (startDate is not null)
            query = query.Where(t => t.TransactionDate >= startDate);

        if (endDate is not null)
            query = query.Where(t => t.TransactionDate <= endDate);

        if (isConfirmed is not null)
            query = query.Where(t => t.IsConfirmed == isConfirmed);

        query = query.OrderByDescending(t => t.TransactionDate)
                     .ThenByDescending(t => t.CreatedAt);

        var count = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<Transaction>(items, count, page, pageSize);
    }

    public async Task<Transaction?> GetByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Transaction>()
            .Include(t => t.Category)
            .Include(t => t.TransactionTags)
                .ThenInclude(tt => tt.Tag)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId && t.DeletedAt == null,
                cancellationToken);
    }

    public async Task<TransactionSummaryResult> GetSummaryForUserAsync(
        Guid userId,
        DateOnly? startDate,
        DateOnly? endDate,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Transaction>()
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.DeletedAt == null);

        if (startDate is not null)
            query = query.Where(t => t.TransactionDate >= startDate);

        if (endDate is not null)
            query = query.Where(t => t.TransactionDate <= endDate);

        var result = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalIncome = g.Where(t => t.Type == TransactionType.Income).Sum(t => (decimal?)t.Amount) ?? 0m,
                TotalExpense = g.Where(t => t.Type == TransactionType.Expense).Sum(t => (decimal?)t.Amount) ?? 0m,
                PendingCount = g.Count(t => !t.IsConfirmed),
                PendingAmount = g.Where(t => !t.IsConfirmed).Sum(t => (decimal?)t.Amount) ?? 0m
            })
            .FirstOrDefaultAsync(cancellationToken);

        return result is null
            ? new TransactionSummaryResult(0m, 0m, 0, 0m)
            : new TransactionSummaryResult(result.TotalIncome, result.TotalExpense, result.PendingCount, result.PendingAmount);
    }

    public async Task<IReadOnlyList<MonthlySummaryResult>> GetMonthlySummaryForUserAsync(
        Guid userId,
        int months,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = new DateOnly(today.AddMonths(-(months - 1)).Year, today.AddMonths(-(months - 1)).Month, 1);

        var items = await Context.Set<Transaction>()
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.DeletedAt == null && t.TransactionDate >= startDate)
            .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Income = g.Where(t => t.Type == TransactionType.Income).Sum(t => (decimal?)t.Amount) ?? 0m,
                Expense = g.Where(t => t.Type == TransactionType.Expense).Sum(t => (decimal?)t.Amount) ?? 0m
            })
            .OrderBy(r => r.Year).ThenBy(r => r.Month)
            .ToListAsync(cancellationToken);

        return items
            .Select(r => new MonthlySummaryResult(r.Year, r.Month, r.Income, r.Expense))
            .ToList()
            .AsReadOnly();
    }

    public async Task<bool> IsTagLinkedAsync(
        Guid transactionId,
        Guid tagId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<TransactionTag>()
            .AnyAsync(tt => tt.TransactionId == transactionId && tt.TagId == tagId, cancellationToken);
    }

    public async Task AddTagAsync(
        Guid transactionId,
        Guid tagId,
        CancellationToken cancellationToken = default)
    {
        var link = TransactionTag.Create(transactionId, tagId);
        await Context.Set<TransactionTag>().AddAsync(link, cancellationToken);
    }

    public async Task RemoveTagAsync(
        Guid transactionId,
        Guid tagId,
        CancellationToken cancellationToken = default)
    {
        var link = await Context.Set<TransactionTag>()
            .FirstOrDefaultAsync(tt => tt.TransactionId == transactionId && tt.TagId == tagId,
                cancellationToken);

        if (link is not null)
            Context.Set<TransactionTag>().Remove(link);
    }
}
