using BudgetWise.Application.Interfaces;
using BudgetWise.Application.Tags.DTOs;
using BudgetWise.Application.Transactions.DTOs;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.Transactions.UseCases;

public sealed class GetTransactionsUseCase(ITransactionRepository repository) : IUseCase
{
    public async Task<Result<PaginatedTransactionResponse>> ExecuteAsync(
        GetTransactionsRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var page = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var paged = await repository.GetTransactionsForUserAsync(
            userId,
            page,
            pageSize,
            request.Type,
            request.CategoryId,
            request.StartDate,
            request.EndDate,
            request.IsConfirmed,
            cancellationToken);

        var response = paged.Map(t =>
        {
            var tags = t.TransactionTags
                .Select(tt => new TagSummary(tt.TagId, tt.Tag.Name))
                .ToList()
                .AsReadOnly();

            return new TransactionResponse(
                t.Id, t.UserId, t.Description, t.Amount, t.Type,
                t.TransactionDate, t.CategoryId, t.Notes, t.RecurrenceType,
                t.RecurrenceEndDate, t.IsConfirmed, t.PaymentMethod,
                t.FamilyGroupId, t.CreatedAt, t.UpdatedAt, tags);
        });

        return PaginatedTransactionResponse.FromPaginatedList(response);
    }
}
