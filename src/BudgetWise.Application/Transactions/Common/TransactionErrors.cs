using BudgetWise.Domain.Common.Results;

namespace BudgetWise.Application.Transactions.Common;

public static class TransactionErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Transaction.NotFound", $"Transação '{id}' não encontrada.");

    public static Error CannotModifyDeleted =>
        Error.Validation("Transaction.CannotModifyDeleted", "Não é possível editar uma transação excluída.");

    public static Error CannotConfirmDeleted =>
        Error.Validation("Transaction.CannotConfirmDeleted", "Não é possível confirmar uma transação excluída.");

    public static Error AlreadyConfirmed =>
        Error.Validation("Transaction.AlreadyConfirmed", "A transação já está confirmada.");
}
