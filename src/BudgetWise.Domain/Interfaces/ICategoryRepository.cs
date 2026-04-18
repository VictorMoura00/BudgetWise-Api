using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Common.Pagination;

namespace BudgetWise.Domain.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    /// <summary>
    /// Retorna categorias do sistema + pessoais ativas do usuário.
    /// Categorias do sistema aparecem primeiro.
    /// </summary>
    Task<PaginatedList<Category>> GetCategoriesForUserAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca categoria por Id garantindo que pertence ao usuário
    /// ou é uma categoria do sistema.
    /// </summary>
    Task<Category?> GetByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se já existe categoria com o mesmo nome no escopo do usuário.
    /// </summary>
    Task<bool> ExistsByNameAsync(
        string name,
        Guid userId,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task ExcludeSystemCategoryForUserAsync(
        Guid userId,
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<bool> IsSystemCategoryExcludedAsync(
        Guid userId,
        Guid categoryId,
        CancellationToken cancellationToken = default);
}