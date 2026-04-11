using BudgetWise.Application.Interfaces;
using BudgetWise.Application.Tags.Common;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.Tags.UseCases;

public sealed class DeleteTagUseCase(
    ITagRepository repository,
    IUnitOfWork unitOfWork) : IUseCase
{
    public async Task<Result> ExecuteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var tag = await repository.GetByIdForUserAsync(id, userId, cancellationToken);

        if (tag is null)
            return Result.Failure(TagErrors.NotFound(id));

        await repository.DeleteAsync(tag.Id, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
