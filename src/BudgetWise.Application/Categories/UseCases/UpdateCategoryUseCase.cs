using BudgetWise.Application.Categories.Common;
using BudgetWise.Application.Categories.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Interfaces;
using BudgetWise.Domain.ValueObjects;

namespace BudgetWise.Application.Categories.UseCases;

public sealed class UpdateCategoryUseCase(
    ICategoryRepository repository,
    IUnitOfWork unitOfWork) : IUseCase
{
    public async Task<Result<CategoryResponse>> ExecuteAsync(
        Guid id,
        UpdateCategoryRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var category = await repository.GetByIdForUserAsync(id, userId, cancellationToken);

        if (category is null)
            return CategoryErrors.NotFound(id);

        if (category.IsSystem)
            return CategoryErrors.CannotModifySystemCategory;

        if (!category.IsActive)
            return CategoryErrors.CannotModifyInactiveCategory;

        var nameExists = await repository.ExistsByNameAsync(request.Name, userId, id, cancellationToken);
        if (nameExists)
            return CategoryErrors.NameAlreadyExists(request.Name);

        HexColor? color = null;
        if (request.Color is not null)
        {
            var colorResult = HexColor.Create(request.Color);
            if (colorResult.IsFailure)
                return colorResult.Error;
            color = colorResult.Value;
        }

        category.Update(request.Name, request.Description, request.Icon, color);

        await repository.UpdateAsync(category, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return new CategoryResponse(
            category.Id, category.Name, category.Description, category.Icon,
            category.Color?.Value, category.IsSystem, category.IsActive,
            category.UserId, category.CreatedAt, category.UpdatedAt);
    }
}
