using BudgetWise.Application.Categories.Common;
using BudgetWise.Application.Categories.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.Categories.UseCases;

public sealed class GetCategoryByIdUseCase(ICategoryRepository repository) : IUseCase
{
    public async Task<Result<CategoryResponse>> ExecuteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var category = await repository.GetByIdForUserAsync(id, userId, cancellationToken);

        if (category is null)
            return CategoryErrors.NotFound(id);

        return new CategoryResponse(
            category.Id, category.Name, category.Description, category.Icon,
            category.Color?.Value, category.IsSystem, category.IsActive,
            category.CategoryType, category.UserId, category.CreatedAt, category.UpdatedAt);
    }
}
