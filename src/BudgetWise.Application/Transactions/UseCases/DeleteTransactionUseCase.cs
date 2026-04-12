using BudgetWise.Application.Interfaces;
using BudgetWise.Application.Transactions.Common;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.Transactions.UseCases;

public sealed class DeleteTransactionUseCase(
    ITransactionRepository repository,
    IUnitOfWork unitOfWork) : IUseCase
{
    public async Task<Result> ExecuteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await repository.GetByIdForUserAsync(id, userId, cancellationToken);

        if (transaction is null)
            return Result.Failure(TransactionErrors.NotFound(id));

        transaction.SoftDelete();

        await repository.UpdateAsync(transaction, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
