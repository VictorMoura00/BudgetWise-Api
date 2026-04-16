using BudgetWise.Application.FamilyGroups.DTOs;
using FluentValidation;

namespace BudgetWise.Application.FamilyGroups.Validators;

public sealed class CreateFamilyGroupRequestValidator : AbstractValidator<CreateFamilyGroupRequest>
{
    public CreateFamilyGroupRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome do grupo é obrigatório.")
            .MaximumLength(100).WithMessage("O nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("A descrição deve ter no máximo 500 caracteres.")
            .When(x => x.Description is not null);
    }
}

public sealed class UpdateFamilyGroupRequestValidator : AbstractValidator<UpdateFamilyGroupRequest>
{
    public UpdateFamilyGroupRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome do grupo é obrigatório.")
            .MaximumLength(100).WithMessage("O nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("A descrição deve ter no máximo 500 caracteres.")
            .When(x => x.Description is not null);
    }
}

public sealed class JoinFamilyGroupRequestValidator : AbstractValidator<JoinFamilyGroupRequest>
{
    public JoinFamilyGroupRequestValidator()
    {
        RuleFor(x => x.InviteCode)
            .NotEmpty().WithMessage("O código de convite é obrigatório.")
            .MaximumLength(20).WithMessage("Código de convite inválido.");
    }
}
