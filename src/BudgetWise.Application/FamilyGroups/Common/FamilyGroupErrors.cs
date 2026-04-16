using BudgetWise.Domain.Common.Results;

namespace BudgetWise.Application.FamilyGroups.Common;

public static class FamilyGroupErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("FamilyGroup.NotFound", $"Grupo familiar '{id}' não encontrado.");

    public static Error NotMember(Guid id) =>
        Error.NotFound("FamilyGroup.NotMember", $"Você não é membro do grupo '{id}'.");

    public static Error MemberNotFound(Guid userId) =>
        Error.NotFound("FamilyGroup.MemberNotFound", $"Membro '{userId}' não encontrado no grupo.");

    public static Error InvalidInviteCode =>
        Error.NotFound("FamilyGroup.InvalidInviteCode", "Código de convite inválido ou expirado.");

    public static Error AlreadyMember =>
        Error.Conflict("FamilyGroup.AlreadyMember", "Você já é membro deste grupo.");

    public static Error GroupLimitReached =>
        Error.Conflict("FamilyGroup.GroupLimitReached", "Limite de 5 grupos por usuário atingido.");

    public static Error NotOwner =>
        Error.Forbidden("FamilyGroup.NotOwner", "Apenas o proprietário pode realizar esta operação.");

    public static Error OwnerCannotLeave =>
        Error.Conflict("FamilyGroup.OwnerCannotLeave", "O proprietário não pode sair sem transferir a propriedade.");

    public static Error CannotRemoveOwner =>
        Error.Conflict("FamilyGroup.CannotRemoveOwner", "O proprietário não pode ser removido do grupo.");
}
