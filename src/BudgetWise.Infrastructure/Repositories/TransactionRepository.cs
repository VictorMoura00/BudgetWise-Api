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
        Guid? familyGroupId = null,
        PaymentMethod? paymentMethod = null,
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

        if (familyGroupId is not null)
            query = query.Where(t => t.FamilyGroupId == familyGroupId);

        if (paymentMethod is not null)
            query = query.Where(t => t.PaymentMethod == paymentMethod);

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
        TransactionType? type = null,
        bool? isConfirmed = null,
        Guid? categoryId = null,
        Guid? familyGroupId = null,
        PaymentMethod? paymentMethod = null,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Transaction>()
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.DeletedAt == null);

        if (startDate is not null)
            query = query.Where(t => t.TransactionDate >= startDate);

        if (endDate is not null)
            query = query.Where(t => t.TransactionDate <= endDate);

        if (type is not null)
            query = query.Where(t => t.Type == type);

        if (isConfirmed is not null)
            query = query.Where(t => t.IsConfirmed == isConfirmed);

        if (categoryId is not null)
            query = query.Where(t => t.CategoryId == categoryId);

        if (familyGroupId is not null)
            query = query.Where(t => t.FamilyGroupId == familyGroupId);

        if (paymentMethod is not null)
            query = query.Where(t => t.PaymentMethod == paymentMethod);

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
        DateOnly startDate,
        DateOnly endDate,
        TransactionType? type = null,
        bool? isConfirmed = null,
        Guid? categoryId = null,
        Guid? familyGroupId = null,
        PaymentMethod? paymentMethod = null,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Transaction>()
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.DeletedAt == null
                     && t.TransactionDate >= startDate && t.TransactionDate <= endDate);

        if (type is not null)
            query = query.Where(t => t.Type == type);

        if (isConfirmed is not null)
            query = query.Where(t => t.IsConfirmed == isConfirmed);

        if (categoryId is not null)
            query = query.Where(t => t.CategoryId == categoryId);

        if (familyGroupId is not null)
            query = query.Where(t => t.FamilyGroupId == familyGroupId);

        if (paymentMethod is not null)
            query = query.Where(t => t.PaymentMethod == paymentMethod);

        var items = await query
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

    public async Task<Transaction?> GetLargestExpenseForUserAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Transaction>()
            .AsNoTracking()
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && t.DeletedAt == null
                     && t.Type == TransactionType.Expense
                     && t.TransactionDate >= startDate && t.TransactionDate <= endDate)
            .OrderByDescending(t => t.Amount)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<MonthProjectionResult> GetMonthProjectionForUserAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var result = await Context.Set<Transaction>()
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.DeletedAt == null
                     && t.TransactionDate >= startDate && t.TransactionDate <= endDate)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                ConfirmedIncome = g.Where(t => t.IsConfirmed && t.Type == TransactionType.Income).Sum(t => (decimal?)t.Amount) ?? 0m,
                ConfirmedExpense = g.Where(t => t.IsConfirmed && t.Type == TransactionType.Expense).Sum(t => (decimal?)t.Amount) ?? 0m,
                PendingIncome = g.Where(t => !t.IsConfirmed && t.Type == TransactionType.Income).Sum(t => (decimal?)t.Amount) ?? 0m,
                PendingExpense = g.Where(t => !t.IsConfirmed && t.Type == TransactionType.Expense).Sum(t => (decimal?)t.Amount) ?? 0m,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result is null)
            return new MonthProjectionResult(0m, 0m, 0m);

        var confirmedBalance = result.ConfirmedIncome - result.ConfirmedExpense;
        var pendingImpact = result.PendingIncome - result.PendingExpense;
        return new MonthProjectionResult(confirmedBalance, pendingImpact, confirmedBalance + pendingImpact);
    }

    public async Task<IReadOnlyList<CategoryComparisonResult>> GetCategoryComparisonForUserAsync(
        Guid userId,
        DateOnly currentStart,
        DateOnly currentEnd,
        DateOnly previousStart,
        DateOnly previousEnd,
        CancellationToken cancellationToken = default)
    {
        var currentData = await Context.Set<Transaction>()
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.DeletedAt == null && t.Type == TransactionType.Expense
                     && t.TransactionDate >= currentStart && t.TransactionDate <= currentEnd)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Amount = g.Sum(t => (decimal?)t.Amount) ?? 0m })
            .ToListAsync(cancellationToken);

        var previousData = await Context.Set<Transaction>()
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.DeletedAt == null && t.Type == TransactionType.Expense
                     && t.TransactionDate >= previousStart && t.TransactionDate <= previousEnd)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Amount = g.Sum(t => (decimal?)t.Amount) ?? 0m })
            .ToListAsync(cancellationToken);

        var categoryIds = currentData.Select(c => c.CategoryId)
            .Concat(previousData.Select(c => c.CategoryId))
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var categoryLookup = categoryIds.Count > 0
            ? await Context.Set<Category>()
                .AsNoTracking()
                .Where(c => categoryIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Name, c.Color })
                .ToDictionaryAsync(c => c.Id, cancellationToken)
            : [];

        var previousDict = previousData.ToDictionary(p => p.CategoryId?.ToString() ?? string.Empty, p => p.Amount);
        var currentDict = currentData.ToDictionary(c => c.CategoryId?.ToString() ?? string.Empty, c => c.Amount);

        var allKeys = currentData.Select(c => c.CategoryId)
            .Union(previousData.Select(c => c.CategoryId))
            .Distinct();

        return allKeys
            .Select(categoryId =>
            {
                string name;
                string? color;
                if (categoryId.HasValue && categoryLookup.TryGetValue(categoryId.Value, out var cat))
                {
                    name = cat.Name;
                    color = cat.Color;
                }
                else
                {
                    name = "Sem categoria";
                    color = null;
                }

                var key = categoryId?.ToString() ?? string.Empty;
                var current = currentDict.GetValueOrDefault(key, 0m);
                var previous = previousDict.GetValueOrDefault(key, 0m);
                return new CategoryComparisonResult(categoryId, name, color, current, previous);
            })
            .OrderByDescending(r => r.CurrentAmount)
            .ToList()
            .AsReadOnly();
    }

    public async Task<IReadOnlyList<Transaction>> GetPendingForDueReportAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Transaction>()
            .AsNoTracking()
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && t.DeletedAt == null && !t.IsConfirmed)
            .OrderBy(t => t.DueDate)
            .ThenBy(t => t.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryTotalResult?> GetTopCategoryByTypeAsync(
        Guid userId,
        TransactionType type,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var top = await Context.Set<Transaction>()
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.DeletedAt == null && t.Type == type
                     && t.TransactionDate >= startDate && t.TransactionDate <= endDate)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Amount = g.Sum(t => (decimal?)t.Amount) ?? 0m })
            .OrderByDescending(g => g.Amount)
            .FirstOrDefaultAsync(cancellationToken);

        if (top is null)
            return null;

        string name = "Sem categoria";
        string? color = null;

        if (top.CategoryId.HasValue)
        {
            var cat = await Context.Set<Category>()
                .AsNoTracking()
                .Where(c => c.Id == top.CategoryId.Value)
                .Select(c => new { c.Name, c.Color })
                .FirstOrDefaultAsync(cancellationToken);

            if (cat is not null)
            {
                name = cat.Name;
                color = cat.Color;
            }
        }

        return new CategoryTotalResult(top.CategoryId, name, color, top.Amount);
    }

    public async Task<IReadOnlyList<CategoryAnalysisResult>> GetCategoryAnalysisForUserAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        TransactionType? type = null,
        bool? isConfirmed = null,
        Guid? categoryId = null,
        Guid? familyGroupId = null,
        PaymentMethod? paymentMethod = null,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Transaction>()
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.DeletedAt == null
                     && t.TransactionDate >= startDate && t.TransactionDate <= endDate);

        if (type is not null)
            query = query.Where(t => t.Type == type);

        if (isConfirmed is not null)
            query = query.Where(t => t.IsConfirmed == isConfirmed);

        if (categoryId is not null)
            query = query.Where(t => t.CategoryId == categoryId);

        if (familyGroupId is not null)
            query = query.Where(t => t.FamilyGroupId == familyGroupId);

        if (paymentMethod is not null)
            query = query.Where(t => t.PaymentMethod == paymentMethod);

        var grouped = await query
            .GroupBy(t => t.CategoryId)
            .Select(g => new
            {
                CategoryId = g.Key,
                TotalAmount = g.Sum(t => (decimal?)t.Amount) ?? 0m,
                TransactionCount = g.Count()
            })
            .OrderByDescending(g => g.TotalAmount)
            .ToListAsync(cancellationToken);

        var categoryIds = grouped
            .Where(g => g.CategoryId.HasValue)
            .Select(g => g.CategoryId!.Value)
            .Distinct()
            .ToList();

        Dictionary<Guid, (string Name, string? Color, string? Icon)> categoryLookup;
        if (categoryIds.Count > 0)
        {
            var cats = await Context.Set<Category>()
                .AsNoTracking()
                .Where(c => categoryIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Name, c.Color, c.Icon })
                .ToListAsync(cancellationToken);
            categoryLookup = cats.ToDictionary(c => c.Id, c => (c.Name, (string?)c.Color, c.Icon));
        }
        else
        {
            categoryLookup = [];
        }

        return grouped
            .Select(g =>
            {
                string name = "Sem categoria";
                string? color = null;
                string? icon = null;

                if (g.CategoryId.HasValue && categoryLookup.TryGetValue(g.CategoryId.Value, out var cat))
                {
                    name = cat.Name;
                    color = cat.Color;
                    icon = cat.Icon;
                }

                return new CategoryAnalysisResult(g.CategoryId, name, color, icon, g.TotalAmount, g.TransactionCount);
            })
            .ToList()
            .AsReadOnly();
    }

    public async Task<IReadOnlyList<Transaction>> GetTransactionsForPaymentStatusAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        TransactionType? type = null,
        Guid? categoryId = null,
        Guid? familyGroupId = null,
        PaymentMethod? paymentMethod = null,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Transaction>()
            .AsNoTracking()
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && t.DeletedAt == null
                     && t.TransactionDate >= startDate && t.TransactionDate <= endDate);

        if (type is not null)
            query = query.Where(t => t.Type == type);

        if (categoryId is not null)
            query = query.Where(t => t.CategoryId == categoryId);

        if (familyGroupId is not null)
            query = query.Where(t => t.FamilyGroupId == familyGroupId);

        if (paymentMethod is not null)
            query = query.Where(t => t.PaymentMethod == paymentMethod);

        return await query
            .OrderBy(t => t.IsConfirmed)
            .ThenBy(t => t.DueDate)
            .ThenBy(t => t.TransactionDate)
            .ToListAsync(cancellationToken);
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
