using BudgetWise.Application.Interfaces;
using BudgetWise.Application.Reports.DTOs;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.Reports.UseCases;

public sealed class GetCategoryAnalysisReportUseCase(
    ITransactionRepository repository,
    TimeProvider timeProvider) : IUseCase
{
    public async Task<Result<CategoryAnalysisReportResponse>> ExecuteAsync(
        Guid userId,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        TransactionType? type = null,
        bool? isConfirmed = null,
        Guid? categoryId = null,
        Guid? familyGroupId = null,
        PaymentMethod? paymentMethod = null,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().DateTime);
        var start = startDate ?? new DateOnly(today.Year, today.Month, 1);
        var end = endDate ?? new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

        if (start > end)
            return Result<CategoryAnalysisReportResponse>.Failure(
                Error.Validation("CategoryAnalysis.InvalidDateRange", "startDate must be less than or equal to endDate."));

        var results = await repository.GetCategoryAnalysisForUserAsync(
            userId, start, end, type, isConfirmed, categoryId, familyGroupId, paymentMethod, cancellationToken);

        var totalAmount = results.Sum(r => r.TotalAmount);
        var totalTransactions = results.Sum(r => r.TransactionCount);

        var items = results
            .Select(r => new CategoryAnalysisItem(
                r.CategoryId,
                r.CategoryName,
                r.CategoryColor,
                r.CategoryIcon,
                r.TotalAmount,
                r.TransactionCount,
                totalAmount > 0 ? Math.Round(r.TotalAmount / totalAmount * 100, 2) : 0m))
            .ToList()
            .AsReadOnly();

        return Result<CategoryAnalysisReportResponse>.Success(new CategoryAnalysisReportResponse(
            start, end, totalAmount, totalTransactions, items));
    }
}
