using BudgetWise.Application.Categories.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.Categories.UseCases;

public sealed class GetCategoriesUseCase(ICategoryRepository repository) : IUseCase
{
    public async Task<Result<PaginatedCategoryResponse>> ExecuteAsync(
        GetCategoriesRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var paged = await repository.GetCategoriesForUserAsync(
            userId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var response = paged.Map(c => new CategoryResponse(
            c.Id, c.Name, c.Description, c.Icon,
            c.Color?.Value, c.IsSystem, c.IsActive,
            c.UserId, c.CreatedAt, c.UpdatedAt));

        return PaginatedCategoryResponse.FromPaginatedList(response);
    }
}
