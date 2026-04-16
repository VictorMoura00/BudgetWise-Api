using BudgetWise.Domain.Common.Results;

namespace BudgetWise.Application.Admin.Common;

public static class AdminErrors
{
    public static Error UserNotFound(Guid id) =>
        Error.NotFound("Admin.UserNotFound", $"Usuário '{id}' não encontrado.");

    public static Error EmailAlreadyInUse(string email) =>
        Error.Conflict("Admin.EmailAlreadyInUse", $"O e-mail '{email}' já está em uso por outro usuário.");

    public static Error CannotSelfDemote =>
        Error.Conflict("Admin.CannotSelfDemote", "Um administrador não pode remover o próprio role de Admin.");

    public static Error InvalidRole(string role) =>
        Error.Validation("Admin.InvalidRole", $"Role '{role}' inválido. Use 'User' ou 'Admin'.");
}
