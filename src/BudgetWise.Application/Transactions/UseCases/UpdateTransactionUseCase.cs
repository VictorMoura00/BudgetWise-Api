using BudgetWise.Application.Interfaces;
using BudgetWise.Application.Tags.DTOs;
using BudgetWise.Application.Transactions.Common;
using BudgetWise.Application.Transactions.DTOs;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.Transactions.UseCases;

public sealed class UpdateTransactionUseCase(
    ITransactionRepository repository,
    IUnitOfWork unitOfWork,
    ICategoryRepository categoryRepository) : IUseCase
{
    public async Task<Result<TransactionResponse>> ExecuteAsync(
        Guid id,
        UpdateTransactionRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await repository.GetByIdForUserAsync(id, userId, cancellationToken);

        if (transaction is null)
            return TransactionErrors.NotFound(id);

        if (request.CategoryId.HasValue)
        {
            var category = await categoryRepository.GetByIdForUserAsync(request.CategoryId.Value, userId, cancellationToken);
            if (category is null)
                return TransactionErrors.CategoryNotFound(request.CategoryId.Value);
            if (!IsCategoryCompatible(category.CategoryType, request.Type))
                return TransactionErrors.IncompatibleCategory(category.Name, request.Type.ToString());
        }

        transaction.Update(
            request.Description,
            request.Amount,
            request.Type,
            request.TransactionDate,
            request.CategoryId,
            request.Notes,
            request.RecurrenceType,
            request.RecurrenceEndDate,
            request.PaymentMethod,
            request.FamilyGroupId,
            request.DueDate);

        await repository.UpdateAsync(transaction, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        var tags = transaction.TransactionTags
            .Select(tt => new TagSummary(tt.TagId, tt.Tag.Name))
            .ToList()
            .AsReadOnly();

        return new TransactionResponse(
            transaction.Id, transaction.UserId, transaction.Description, transaction.Amount,
            transaction.Type, transaction.TransactionDate, transaction.DueDate,
            transaction.CategoryId, transaction.Category?.Name, transaction.Category?.Color,
            transaction.Notes, transaction.RecurrenceType, transaction.RecurrenceEndDate,
            transaction.IsConfirmed, transaction.PaidAt, transaction.PaymentMethod,
            transaction.FamilyGroupId, transaction.CreatedAt, transaction.UpdatedAt, tags);
    }

    private static bool IsCategoryCompatible(CategoryType categoryType, TransactionType transactionType) =>
        categoryType == CategoryType.Both ||
        (categoryType == CategoryType.Expense && transactionType == TransactionType.Expense) ||
        (categoryType == CategoryType.Income && transactionType == TransactionType.Income);
}
