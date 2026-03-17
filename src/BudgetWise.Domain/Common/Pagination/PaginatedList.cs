namespace BudgetWise.Domain.Common.Pagination;

public class PaginatedList<T>(IEnumerable<T> items, long count, int pageNumber, int pageSize)
{
    public IReadOnlyCollection<T> Items { get; } = items.ToList().AsReadOnly();
    public int PageNumber { get; } = pageNumber;
    public int PageSize { get; } = pageSize;
    public long TotalCount { get; } = count;

    public int TotalPages { get; } = pageSize > 0 ? (int)Math.Ceiling(count / (double)pageSize) : 0;

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

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
        var mappedItems = Items.Select(mapFunc);
        return new PaginatedList<TResult>(mappedItems, TotalCount, PageNumber, PageSize);
    }
}
