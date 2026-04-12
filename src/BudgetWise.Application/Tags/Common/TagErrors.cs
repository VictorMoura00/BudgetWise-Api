using BudgetWise.Domain.Common.Results;

namespace BudgetWise.Application.Tags.Common;

public static class TagErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Tag.NotFound", $"Tag '{id}' não encontrada.");

    public static Error DuplicateName(string name) =>
        Error.Conflict("Tag.DuplicateName", $"Já existe uma tag com o nome '{name}'.");

    public static Error AlreadyLinked =>
        Error.Conflict("Tag.AlreadyLinked", "Esta tag já está vinculada à transação.");

    public static Error NotLinked =>
        Error.NotFound("Tag.NotLinked", "Esta tag não está vinculada à transação.");
}
