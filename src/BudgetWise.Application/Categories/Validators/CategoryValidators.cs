using BudgetWise.Application.Categories.DTOs;
using FluentValidation;

namespace BudgetWise.Application.Categories.Validators;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Color)
            .Matches(@"^#[0-9A-Fa-f]{6}$")
            .When(x => !string.IsNullOrEmpty(x.Color))
            .WithMessage("Cor deve ser um hexadecimal válido (ex: #FF5733).");
    }
}

public sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Color)
            .Matches(@"^#[0-9A-Fa-f]{6}$")
            .When(x => !string.IsNullOrEmpty(x.Color))
            .WithMessage("Cor deve ser um hexadecimal válido (ex: #FF5733).");
    }
}
