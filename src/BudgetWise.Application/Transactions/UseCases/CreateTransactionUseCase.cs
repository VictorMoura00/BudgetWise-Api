using BudgetWise.Application.Interfaces;
using BudgetWise.Application.Tags.DTOs;
using BudgetWise.Application.Transactions.DTOs;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.Transactions.UseCases;

public sealed class CreateTransactionUseCase(
    ITransactionRepository repository,
    IUnitOfWork unitOfWork) : IUseCase
{
    public async Task<Result<TransactionResponse>> ExecuteAsync(
        CreateTransactionRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var transaction = Transaction.Create(
            userId,
            request.Description,
            request.Amount,
            request.Type,
            request.TransactionDate,
            request.CategoryId,
            request.Notes,
            request.RecurrenceType,
            request.RecurrenceEndDate,
            request.IsConfirmed,
            request.PaymentMethod,
            request.FamilyGroupId);

        await repository.AddAsync(transaction, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return new TransactionResponse(
            transaction.Id, transaction.UserId, transaction.Description, transaction.Amount,
            transaction.Type, transaction.TransactionDate, transaction.CategoryId,
            transaction.Notes, transaction.RecurrenceType, transaction.RecurrenceEndDate,
            transaction.IsConfirmed, transaction.PaymentMethod, transaction.FamilyGroupId,
            transaction.CreatedAt, transaction.UpdatedAt,
            []);
    }
}
