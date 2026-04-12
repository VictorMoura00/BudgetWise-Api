using BudgetWise.Application.Interfaces;
using BudgetWise.Application.Tags.Common;
using BudgetWise.Application.Transactions.Common;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.Transactions.UseCases;

public sealed class AddTagToTransactionUseCase(
    ITransactionRepository transactionRepository,
    ITagRepository tagRepository,
    IUnitOfWork unitOfWork) : IUseCase
{
    public async Task<Result> ExecuteAsync(
        Guid transactionId,
        Guid tagId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await transactionRepository.GetByIdForUserAsync(transactionId, userId, cancellationToken);

        if (transaction is null)
            return Result.Failure(TransactionErrors.NotFound(transactionId));

        var tag = await tagRepository.GetByIdForUserAsync(tagId, userId, cancellationToken);

        if (tag is null)
            return Result.Failure(TagErrors.NotFound(tagId));

        if (await transactionRepository.IsTagLinkedAsync(transactionId, tagId, cancellationToken))
            return Result.Failure(TagErrors.AlreadyLinked);

        await transactionRepository.AddTagAsync(transactionId, tagId, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
