using BudgetWise.Domain.Common.Pagination;
using BudgetWise.Domain.Enums;

namespace BudgetWise.Application.Categories.DTOs;

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    string? Icon,
    string? Color,
    bool IsSystem,
    bool IsActive,
    CategoryType CategoryType,
    Guid? UserId,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public sealed record CreateCategoryRequest(
    string Name,
    string? Description,
    string? Icon,
    string? Color,
    CategoryType CategoryType = CategoryType.Both
);

public sealed record UpdateCategoryRequest(
    string Name,
    string? Description,
    string? Icon,
    string? Color,
    CategoryType CategoryType = CategoryType.Both
);

public sealed record GetCategoriesRequest(
    int PageNumber = 1,
    int PageSize = 20
);

public sealed record PaginatedCategoryResponse(
    IReadOnlyCollection<CategoryResponse> Items,
    int PageNumber,
    int PageSize,
    long TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage
)
{
    public static PaginatedCategoryResponse FromPaginatedList(PaginatedList<CategoryResponse> list) => new(
        Items: list.Items,
        PageNumber: list.PageNumber,
        PageSize: list.PageSize,
        TotalCount: list.TotalCount,
        TotalPages: list.TotalPages,
        HasPreviousPage: list.HasPreviousPage,
        HasNextPage: list.HasNextPage
    );
}
