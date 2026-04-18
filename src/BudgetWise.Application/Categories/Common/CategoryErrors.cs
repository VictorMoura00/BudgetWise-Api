using BudgetWise.Domain.Common.Results;

namespace BudgetWise.Application.Categories.Common;

public static class CategoryErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Category.NotFound", $"Categoria '{id}' não encontrada.");

    public static Error NameAlreadyExists(string name) =>
        Error.Conflict("Category.NameAlreadyExists", $"Já existe uma categoria com o nome '{name}'.");

    public static Error CannotModifySystemCategory =>
        Error.Validation("Category.CannotModifySystemCategory", "Categorias do sistema não podem ser editadas.");

    public static Error SystemCategoryAlreadyExcluded =>
        Error.Conflict("Category.AlreadyExcluded", "Esta categoria do sistema já foi removida da sua lista.");

    public static Error CannotModifyInactiveCategory =>
        Error.Validation("Category.CannotModifyInactiveCategory", "Categorias inativas não podem ser modificadas.");
}
