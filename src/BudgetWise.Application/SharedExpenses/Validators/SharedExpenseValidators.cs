using BudgetWise.Application.SharedExpenses.DTOs;
using FluentValidation;

namespace BudgetWise.Application.SharedExpenses.Validators;

public sealed class CreateSharedExpenseRequestValidator : AbstractValidator<CreateSharedExpenseRequest>
{
    public CreateSharedExpenseRequestValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Descrição é obrigatória.")
            .MaximumLength(255).WithMessage("Descrição deve ter no máximo 255 caracteres.");

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0).WithMessage("Valor total deve ser maior que zero.");

        RuleFor(x => x.Participants)
            .NotEmpty().WithMessage("É necessário ao menos um participante.");

        RuleForEach(x => x.Participants).ChildRules(p =>
        {
            p.RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId do participante é obrigatório.");

            p.RuleFor(x => x.AmountOwed)
                .GreaterThan(0).WithMessage("Valor de cada participante deve ser maior que zero.");
        });
    }
}
