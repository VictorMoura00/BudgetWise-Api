using BudgetWise.Domain.Common.Results;

namespace BudgetWise.Application.SharedExpenses.Common;

public static class SharedExpenseErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("SharedExpense.NotFound", $"Despesa compartilhada '{id}' não encontrada.");

    public static Error ParticipantNotFound(Guid userId) =>
        Error.NotFound("SharedExpense.ParticipantNotFound", $"Participante '{userId}' não encontrado nesta despesa.");

    public static Error AlreadySettled =>
        Error.Validation("SharedExpense.AlreadySettled", "Este participante já liquidou sua parte.");

    public static Error IncompatibleCategory(string categoryName) =>
        Error.Validation("SharedExpense.IncompatibleCategory",
            $"A categoria '{categoryName}' não é compatível com despesas compartilhadas (use Expense ou Both).");

    public static Error CategoryNotFound(Guid id) =>
        Error.NotFound("SharedExpense.CategoryNotFound", $"Categoria '{id}' não encontrada.");
}
