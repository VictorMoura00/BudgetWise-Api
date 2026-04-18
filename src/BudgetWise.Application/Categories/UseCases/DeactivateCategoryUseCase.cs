using BudgetWise.Application.Categories.Common;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.Categories.UseCases;

public sealed class DeactivateCategoryUseCase(
    ICategoryRepository repository,
    IUnitOfWork unitOfWork) : IUseCase
{
    public async Task<Result> ExecuteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var category = await repository.GetByIdForUserAsync(id, userId, cancellationToken);

        if (category is null)
            return Result.Failure(CategoryErrors.NotFound(id));

        if (category.IsSystem)
        {
            var alreadyExcluded = await repository.IsSystemCategoryExcludedAsync(userId, id, cancellationToken);
            if (alreadyExcluded)
                return Result.Failure(CategoryErrors.SystemCategoryAlreadyExcluded);

            await repository.ExcludeSystemCategoryForUserAsync(userId, id, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return Result.Success();
        }

        if (!category.IsActive)
            return Result.Failure(CategoryErrors.CannotModifyInactiveCategory);

        category.Deactivate();
        await repository.UpdateAsync(category, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
