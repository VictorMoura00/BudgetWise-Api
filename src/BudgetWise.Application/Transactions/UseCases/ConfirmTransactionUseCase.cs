using BudgetWise.Application.Interfaces;
using BudgetWise.Application.Tags.DTOs;
using BudgetWise.Application.Transactions.Common;
using BudgetWise.Application.Transactions.DTOs;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.Transactions.UseCases;

public sealed class ConfirmTransactionUseCase(
    ITransactionRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IUseCase
{
    public async Task<Result<TransactionResponse>> ExecuteAsync(
        Guid id,
        DateOnly? paidAt,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await repository.GetByIdForUserAsync(id, userId, cancellationToken);

        if (transaction is null)
            return TransactionErrors.NotFound(id);

        if (transaction.IsConfirmed)
            return TransactionErrors.AlreadyConfirmed;

        var dataPagamento = paidAt ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().DateTime);
        transaction.Confirm(dataPagamento);

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
}
