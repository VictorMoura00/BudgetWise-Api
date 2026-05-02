using System.Text.Json.Serialization;

namespace BudgetWise.Domain.Common.Pagination;

public class PaginatedList<T>
{
    public IReadOnlyCollection<T> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public long TotalCount { get; init; }

    public int TotalPages { get; init; }

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PaginatedList()
    {
    }

    public PaginatedList(IReadOnlyCollection<T> items, long totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;
    }

    /// <summary>
    /// Transforma uma lista paginada de Entidades em uma lista paginada de DTOs.
    /// <example>
    /// <br>Como chamar o método Map para converter uma lista paginada de categorias em uma lista paginada de CategoryDto:</br>
    /// <code>
    /// var response = pagedCategories.Map(c => new Categor'yDto(c.Id, c.Name));
    /// </code>
    /// </example>
    /// </summary>
    public PaginatedList<TResult> Map<TResult>(Func<T, TResult> mapFunc)
    {
        var mappedItems = Items.Select(mapFunc).ToList();
        return new PaginatedList<TResult>(mappedItems, TotalCount, PageNumber, PageSize);
    }
}
