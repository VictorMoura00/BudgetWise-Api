using BudgetWise.Application.Categories.Common;
using BudgetWise.Application.Categories.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Interfaces;
using BudgetWise.Domain.ValueObjects;

namespace BudgetWise.Application.Categories.UseCases;

public sealed class CreateCategoryUseCase(
    ICategoryRepository repository,
    IUnitOfWork unitOfWork) : IUseCase
{
    public async Task<Result<CategoryResponse>> ExecuteAsync(
        CreateCategoryRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var nameExists = await repository.ExistsByNameAsync(request.Name, userId, cancellationToken: cancellationToken);
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

        var category = Category.CreatePersonal(userId, request.Name, request.Description, request.Icon, color, request.CategoryType);

        await repository.AddAsync(category, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return new CategoryResponse(
            category.Id, category.Name, category.Description, category.Icon,
            category.Color?.Value, category.IsSystem, category.IsActive,
            category.CategoryType, category.UserId, category.CreatedAt, category.UpdatedAt);
    }
}
